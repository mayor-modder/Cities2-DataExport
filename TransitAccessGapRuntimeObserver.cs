using System;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace CS2DataExport;

using NetOutsideConnection = Game.Net.OutsideConnection;
using ObjectOutsideConnection = Game.Objects.OutsideConnection;
using ObjectTransform = Game.Objects.Transform;
using RouteTransportStop = Game.Routes.TransportStop;

public sealed class TransitAccessGapRuntimeObserver
{
    private const ulong TripGraceTicks = 2048;
    private const string NoProvenPassengerCarrierNote = "no proven passenger-trip runtime carrier";

    private readonly TransitAccessGapCaptureCoordinator _coordinator;
    private readonly Dictionary<Entity, ActiveTrip> _activeTrips = new();
    private ulong _observeTick;

    public TransitAccessGapRuntimeObserver(TransitAccessGapCaptureCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public void Observe(EntityManager entityManager, ExportSettings settings)
    {
        if (!_coordinator.IsCaptureActive)
        {
            return;
        }

        if (!HasProvenPassengerTripCarrier())
        {
            _coordinator.MarkPassengerTripCarrierUnavailable(NoProvenPassengerCarrierNote);
            return;
        }

        _observeTick++;
        ReplaceStops(entityManager);

        // Ready path once we can prove a passenger-only trip carrier at runtime.
        EntityQueryDesc queryDesc = new()
        {
            All = new[]
            {
                ComponentType.ReadOnly<PathOwner>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<Deleted>()
            }
        };

        EntityQuery query = entityManager.CreateEntityQuery(queryDesc);
        using NativeArray<Entity> humans = query.ToEntityArray(Allocator.Temp);
        for (int index = 0; index < humans.Length; index++)
        {
            ProcessHuman(entityManager, humans[index], settings);
        }

        PruneInactiveTrips();
    }

    private void ProcessHuman(EntityManager entityManager, Entity human, ExportSettings settings)
    {
        if (!entityManager.HasBuffer<PathElement>(human))
        {
            return;
        }

        DynamicBuffer<PathElement> pathElements = entityManager.GetBuffer<PathElement>(human);
        if (!HasUsablePath(pathElements))
        {
            return;
        }

        Entity target = entityManager.HasComponent<Target>(human)
            ? entityManager.GetComponentData<Target>(human).m_Target
            : Entity.Null;
        Entity destination = ResolveLastPathTarget(pathElements);
        bool includesOutsideConnection = IsOutsideConnection(entityManager, target) || IsOutsideConnection(entityManager, destination);

        if (_activeTrips.TryGetValue(human, out ActiveTrip activeTrip))
        {
            if (activeTrip.Target != target || activeTrip.Destination != destination)
            {
                _activeTrips.Remove(human);
                RegisterTrip(entityManager, human, target, destination, pathElements, includesOutsideConnection);
                return;
            }

            _activeTrips[human] = activeTrip with { LastObservedTick = _observeTick };
            return;
        }

        RegisterTrip(entityManager, human, target, destination, pathElements, includesOutsideConnection);
    }

    private void RegisterTrip(
        EntityManager entityManager,
        Entity human,
        Entity target,
        Entity destination,
        DynamicBuffer<PathElement> pathElements,
        bool includesOutsideConnection)
    {
        var trip = new CapturedTransitTrip
        {
            IncludesOutsideConnection = includesOutsideConnection
        };

        if (TryResolveAnchor(entityManager, ResolveFirstPathTarget(pathElements), out TransitAccessGapAnchor originAnchor))
        {
            trip.Anchors.Add(originAnchor);
        }

        Entity preferredDestination = destination != Entity.Null ? destination : target;
        if (TryResolveAnchor(entityManager, preferredDestination, out TransitAccessGapAnchor destinationAnchor))
        {
            trip.Anchors.Add(destinationAnchor);
        }

        for (int index = 0; index < pathElements.Length; index++)
        {
            Entity routeEntity = pathElements[index].m_Target;
            if (routeEntity == Entity.Null)
            {
                continue;
            }

            trip.RouteSegments.Add(new TransitAccessGapRouteSegmentRecord(routeEntity.Index, routeEntity.Version, null));
        }

        if (trip.Anchors.Count > 0)
        {
            _coordinator.RecordTrip(trip);
        }

        _activeTrips[human] = new ActiveTrip(target, destination, _observeTick);
    }

    private void ReplaceStops(EntityManager entityManager)
    {
        EntityQueryDesc queryDesc = new()
        {
            All = new[]
            {
                ComponentType.ReadOnly<RouteTransportStop>(),
                ComponentType.ReadOnly<PrefabRef>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<Deleted>()
            }
        };

        EntityQuery query = entityManager.CreateEntityQuery(queryDesc);
        using NativeArray<Entity> stops = query.ToEntityArray(Allocator.Temp);
        var observedStops = new List<TransitAccessGapStop>(stops.Length);

        for (int index = 0; index < stops.Length; index++)
        {
            Entity stop = stops[index];
            PrefabRef prefabRef = entityManager.GetComponentData<PrefabRef>(stop);
            bool includeStop = !entityManager.HasComponent<TransportStopData>(prefabRef.m_Prefab)
                || entityManager.GetComponentData<TransportStopData>(prefabRef.m_Prefab).m_PassengerTransport;

            if (includeStop && TryResolveAnchor(entityManager, stop, out TransitAccessGapAnchor anchor))
            {
                observedStops.Add(new TransitAccessGapStop(anchor.X, anchor.Y, anchor.Z, 250));
            }
        }

        _coordinator.ReplaceStops(observedStops);
    }

    private static bool HasUsablePath(DynamicBuffer<PathElement> pathElements)
    {
        if (pathElements.Length < 2)
        {
            return false;
        }

        Entity previous = pathElements[0].m_Target;
        for (int index = 1; index < pathElements.Length; index++)
        {
            Entity current = pathElements[index].m_Target;
            if (previous != Entity.Null && current != Entity.Null && previous != current)
            {
                return true;
            }

            previous = current;
        }

        return false;
    }

    private static Entity ResolveFirstPathTarget(DynamicBuffer<PathElement> pathElements)
    {
        for (int index = 0; index < pathElements.Length; index++)
        {
            if (pathElements[index].m_Target != Entity.Null)
            {
                return pathElements[index].m_Target;
            }
        }

        return Entity.Null;
    }

    private static Entity ResolveLastPathTarget(DynamicBuffer<PathElement> pathElements)
    {
        for (int index = pathElements.Length - 1; index >= 0; index--)
        {
            if (pathElements[index].m_Target != Entity.Null)
            {
                return pathElements[index].m_Target;
            }
        }

        return Entity.Null;
    }

    private static bool TryResolveAnchor(EntityManager entityManager, Entity entity, out TransitAccessGapAnchor anchor)
    {
        Entity current = entity;
        for (int depth = 0; depth < 6 && current != Entity.Null; depth++)
        {
            if (entityManager.HasComponent<ObjectTransform>(current))
            {
                var transform = entityManager.GetComponentData<ObjectTransform>(current);
                anchor = new TransitAccessGapAnchor(transform.m_Position.x, transform.m_Position.y, transform.m_Position.z);
                return true;
            }

            if (!entityManager.HasComponent<Owner>(current))
            {
                break;
            }

            Entity owner = entityManager.GetComponentData<Owner>(current).m_Owner;
            if (owner == Entity.Null || owner == current)
            {
                break;
            }

            current = owner;
        }

        anchor = new TransitAccessGapAnchor(0, 0, 0);
        return false;
    }

    private static bool IsOutsideConnection(EntityManager entityManager, Entity entity)
    {
        Entity current = entity;
        for (int depth = 0; depth < 6 && current != Entity.Null; depth++)
        {
            if (entityManager.HasComponent<NetOutsideConnection>(current) || entityManager.HasComponent<ObjectOutsideConnection>(current))
            {
                return true;
            }

            if (!entityManager.HasComponent<Owner>(current))
            {
                return false;
            }

            Entity owner = entityManager.GetComponentData<Owner>(current).m_Owner;
            if (owner == Entity.Null || owner == current)
            {
                return false;
            }

            current = owner;
        }

        return false;
    }

    private void PruneInactiveTrips()
    {
        if (_activeTrips.Count == 0)
        {
            return;
        }

        var removals = new List<Entity>();
        foreach (KeyValuePair<Entity, ActiveTrip> pair in _activeTrips)
        {
            if (_observeTick - pair.Value.LastObservedTick > TripGraceTicks)
            {
                removals.Add(pair.Key);
            }
        }

        for (int index = 0; index < removals.Count; index++)
        {
            _activeTrips.Remove(removals[index]);
        }
    }

    private static bool HasProvenPassengerTripCarrier()
    {
        // Safe fallback: keep this false until we validate a runtime carrier that distinguishes
        // passenger trips from freight/service traffic. Candidate families to investigate next
        // include Citizen, HumanCurrentLane, TravelPurpose, and ownership-linked human trip paths.
        return false;
    }

    private readonly record struct ActiveTrip(Entity Target, Entity Destination, ulong LastObservedTick);
}
