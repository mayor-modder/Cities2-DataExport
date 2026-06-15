# CitySnapshotV1

Schema version: `2.7.0`

`2.7.0` is an additive refresh over `2.6.0`:
- adds `official_city_statistics` for official aggregate counters from managed game systems and city singleton components

`2.6.0` is an additive refresh over `2.5.0`:
- adds `transit_line_detail_semantics` for XTM-style live per-line stop queues, onboard vehicle load, average occupancy, expected round-trip time, and next-maintenance vehicle evidence

`2.5.0` is an additive refresh over `2.4.0`:
- adds `transit_access_gap_semantics` for capture-window transit hotspot, stop-coverage, and sampled route evidence
- adds `capture_window` as a supported `metric_metadata.time_basis` value

`2.4.0` is an additive refresh over `2.3.0`:
- adds `transit_performance_semantics` for line-first runtime transit pressure, mode pressure, and service-gap interpretation

`2.3.0` is an additive refresh over `2.2.0`:
- adds `housing_pressure_semantics`, `household_pressure_context`, and `labor_pressure_context` groups for one-reload live housing/labor interpretation

`2.2.0` is an additive refresh over `2.1.0`:
- adds a `company_service_semantics` group for live provider-side staffing pressure by sector

`2.1.0` is an additive refresh over `2.0.0`:
- adds a `facility_identity` group for live building/facility meaning

`2.0.0` is a breaking refresh over `1.4.0`:
- mobility line exports are now observed from runtime line UI/ECS state
- mobility proxy usage/passenger-load/monthly-estimate fields are removed
- economy profitability proxy fields are removed
- `meta.source` now defaults to `ecs_observed`

## Top-level fields

- `schema_version` (`string`)
- `exported_at_utc` (`string`, ISO-8601 UTC)
- `game_build` (`string|null`)
- `mod_version` (`string`)
- `city` (`object`)
- `population` (`object`)
- `official_city_statistics` (`object`)
- `education` (`object`)
- `transport_proxies` (`object`)
- `workforce` (`object`)
- `workplaces` (`object`)
- `mobility` (`object`)
- `economy_signals` (`object`)
- `external_connections` (`object`)
- `labor_market_detail` (`object`)
- `facility_identity` (`object`)
- `company_service_semantics` (`object`)
- `housing_pressure_semantics` (`object`)
- `household_pressure_context` (`object`)
- `labor_pressure_context` (`object`)
- `transit_performance_semantics` (`object`)
- `transit_line_detail_semantics` (`object`)
- `transit_access_gap_semantics` (`object`)
- `meta` (`object`)

## Group Status Contract

Each group includes:
- `status`: `ok|partial|unavailable`
- `notes`: `string[]`

Unavailable metrics remain present with `null`.

## Existing Groups (unchanged keys)

- `city`
  - `city_name` (`string|null`)
  - `district_count` (`number|null`)
  - `building_count` (`number|null`)
  - `source_component` (`string`)

- `population`
  - `total_population` (`number|null`)
  - `household_count` (`number|null`)
  - `birth_rate_per_month` (`number|null`)
  - `death_rate_per_month` (`number|null`)
  - `local_population` (`number|null`)
  - `tourist_population` (`number|null`)
  - `commuter_population` (`number|null`)
  - `moving_away_population` (`number|null`)
  - `homeless_population` (`number|null`)
  - `working_age_population` (`number|null`)
  - `children_population` (`number|null`)
  - `elderly_population` (`number|null`)
  - `source_component` (`string`)

## Group: `official_city_statistics` (`2.7.0`)

- `time` (`official_time_statistics`)
- `finance` (`official_finance_statistics`)
- `taxes` (`official_tax_statistics`)
- `population_flow` (`official_population_flow_statistics`)
- `social` (`official_social_statistics`)
- `tourism` (`official_tourism_statistics`)
- `transport_totals` (`official_transport_totals_statistics`)
- `sectors` (`official_sector_statistics`)
- `city_services` (`official_city_service_statistics`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

- `education`
  - `educated_percent` (`number|null`, `%`)
  - `highly_educated_percent` (`number|null`, `%`)
  - `employment_rate_percent` (`number|null`, `%`)
  - `levels` (`array<workforce_level_summary>`)
  - `source_component` (`string`)

- `transport_proxies`
  - `road_vehicle_entities` (`number|null`)
  - `public_transport_vehicle_entities` (`number|null`)
  - `active_transport_lines` (`number|null`)
  - `congestion_index_0_to_1` (`number|null`, `0..1`)
  - `source_component` (`string`)

- `workforce`
  - `total_potential_workers` (`number|null`)
  - `workers` (`number|null`)
  - `unemployed` (`number|null`)
  - `homeless_unemployed` (`number|null`)
  - `employable` (`number|null`)
  - `outside_workers` (`number|null`)
  - `underemployed_workers` (`number|null`)
  - `levels` (`array<workforce_level_summary>`)
  - `source_component` (`string`)

- `workplaces`
  - `total_workplaces` (`number|null`)
  - `filled_workplaces` (`number|null`)
  - `open_workplaces` (`number|null`)
  - `commuter_employees` (`number|null`)
  - `work_providers_total` (`number|null`)
  - `work_providers_service` (`number|null`)
  - `work_providers_commercial` (`number|null`)
  - `work_providers_leisure` (`number|null`)
  - `work_providers_extractor` (`number|null`)
  - `work_providers_industrial` (`number|null`)
  - `work_providers_office` (`number|null`)
  - `levels` (`array<workplace_level_summary>`)
  - `source_component` (`string`)

## Group: `mobility` (`2.0.0`)

- `traffic_volume_index` (`number|null`, `index`)
- `lines_total` (`number|null`, `count`)
- `passenger_lines_total` (`number|null`, `count`)
- `cargo_lines_total` (`number|null`, `count`)
- `lines_by_transport_type` (`mode_entity_counts|null`)
- `active_vehicles_by_transport_type` (`mode_entity_counts|null`)
- `lines_with_service_count` (`number|null`, `count`)
- `lines_without_service_count` (`number|null`, `count`)
- `lines_with_service_percent` (`number|null`, `%`)
- `line_vehicle_entities_p50` (`number|null`, `count`)
- `line_vehicle_entities_p95` (`number|null`, `count`)
- `top_lines_by_active_vehicles` (`array<mobility_line_record>`)
- `lines` (`array<mobility_line_record>`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `economy_signals` (`2.0.0`)

- `land_value_avg` (`number|null`, `index`)
- `land_value_p25` (`number|null`, `index`)
- `land_value_p50` (`number|null`, `index`)
- `land_value_p75` (`number|null`, `index`)
- `citizen_wealth_avg` (`number|null`, `currency`)
- `citizen_wealth_p25` (`number|null`, `currency`)
- `citizen_wealth_p50` (`number|null`, `currency`)
- `citizen_wealth_p75` (`number|null`, `currency`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `external_connections`

- `imports_total_value` (`number|null`, `currency/month`)
- `exports_total_value` (`number|null`, `currency/month`)
- `imports_by_resource` (`object<string, number|null>|null`)
- `exports_by_resource` (`object<string, number|null>|null`)
- `service_trade` (`object<string, number|null>|null`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `labor_market_detail`

- `jobs_available_by_education_level` (`level_count_summary|null`)
- `jobs_filled_by_education_level` (`level_count_summary|null`)
- `jobs_open_by_education_level` (`level_count_summary|null`)
- `workforce_by_education_level` (`workforce_by_education_summary|null`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `facility_identity`

- `total_building_entities` (`number|null`, `count`)
- `residential_building_entities` (`number|null`, `count`)
- `transport_building_entities` (`number|null`, `count`)
- `active_work_provider_entities` (`number|null`, `count`)
- `service_provider_entities` (`number|null`, `count`)
- `commercial_provider_entities` (`number|null`, `count`)
- `leisure_provider_entities` (`number|null`, `count`)
- `extractor_provider_entities` (`number|null`, `count`)
- `industrial_provider_entities` (`number|null`, `count`)
- `office_provider_entities` (`number|null`, `count`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `company_service_semantics`

- `provider_counts` (`sector_int_summary`)
- `jobs_total` (`sector_int_summary`)
- `jobs_filled` (`sector_int_summary`)
- `jobs_open` (`sector_int_summary`)
- `fill_percent` (`sector_double_summary`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `housing_pressure_semantics`

- `total_households` (`number|null`, `count`)
- `residential_building_entities` (`number|null`, `count`)
- `local_households` (`number|null`, `count`)
- `homeless_households` (`number|null`, `count`)
- `moving_away_households` (`number|null`, `count`)
- `households_per_residential_building` (`number|null`)
- `local_households_per_residential_building` (`number|null`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `household_pressure_context`

- `total_households` (`number|null`, `count`)
- `local_households` (`number|null`, `count`)
- `property_linked_households` (`number|null`, `count`)
- `homeless_households` (`number|null`, `count`)
- `moving_away_households` (`number|null`, `count`)
- `homeless_household_share_percent` (`number|null`, `%`)
- `moving_away_household_share_percent` (`number|null`, `%`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `labor_pressure_context`

- `total_potential_workers` (`number|null`, `workers`)
- `total_jobs` (`number|null`, `jobs`)
- `jobs_minus_potential_workers` (`number|null`, `jobs`)
- `jobs_minus_current_workers` (`number|null`, `jobs`)
- `outside_worker_share_percent` (`number|null`, `%`)
- `underemployed_worker_share_percent` (`number|null`, `%`)
- `commuter_employee_share_percent` (`number|null`, `%`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `transit_performance_semantics`

- `line_pressure` (`object`)
  - `usage_observed_lines` (`number|null`, `count`)
  - `capacity_observed_lines` (`number|null`, `count`)
  - `high_pressure_lines` (`number|null`, `count`, derived using `usage_percent >= 75`)
  - `critical_pressure_lines` (`number|null`, `count`, derived using `usage_percent >= 90`)
  - `top_pressure_lines` (`transit_pressure_line_summary[]`)
- `mode_pressure` (`object`)
  - `average_usage_percent_by_mode` (`mode_double_values`)
  - `high_pressure_lines_by_mode` (`mode_entity_counts`)
  - `usage_observed_lines_by_mode` (`mode_entity_counts`)
- `service_gaps` (`object`)
  - `no_service_lines` (`number|null`, `count`)
  - `thin_service_lines` (`number|null`, `count`)
  - `missing_usage_observed_lines` (`number|null`, `count`)
  - `missing_capacity_observed_lines` (`number|null`, `count`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

## Group: `transit_line_detail_semantics` (`2.6.0`)

- `lines_observed` (`number|null`, `count`)
- `passenger_lines_observed` (`number|null`, `count`)
- `cargo_lines_observed` (`number|null`, `count`)
- `total_waiting_passengers` (`number|null`, `passengers`)
- `total_onboard_passengers` (`number|null`, `passengers`)
- `max_waiting_passengers_at_stop` (`number|null`, `passengers`)
- `lines` (`array<transit_line_detail_record>`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

### `transit_line_detail_record`

- `line_entity_index` (`number`)
- `line_entity_version` (`number`)
- `line_name` (`string|null`)
- `line_identifier` (`string|null`)
- `line_identifier_source` (`string`)
- `route_number` (`number|null`)
- `mode` (`string`)
- `is_cargo` (`bool`)
- `line_color` (`string|null`)
- `active` (`bool`)
- `visible` (`bool`)
- `stop_count` (`number`)
- `segment_count` (`number`)
- `active_vehicle_entities` (`number`)
- `stop_capacity` (`number|null`)
- `waiting_passengers_all_stops` (`number`, passengers)
- `max_waiting_passengers_at_stop` (`number`, passengers)
- `onboard_passengers_in_vehicles` (`number`, passengers)
- `total_passenger_capacity` (`number|null`, passengers)
- `average_vehicle_occupancy_percent` (`number|null`, `%`)
- `average_stop_occupancy_percent` (`number|null`, `%`)
- `expected_round_trip_time_ticks` (`number|null`, simulation ticks)
- `expected_round_trip_time_minutes` (`number|null`, game minutes)
- `next_maintenance_vehicle_entity_index` (`number|null`)
- `next_maintenance_vehicle_entity_version` (`number|null`)
- `next_maintenance_distance_m` (`number|null`, meters)
- `stops` (`array<transit_line_stop_detail_record>`)

### `transit_line_stop_detail_record`

- `waypoint_entity_index` (`number`)
- `waypoint_entity_version` (`number`)
- `stop_name` (`string|null`)
- `waiting_passengers` (`number`, passengers)
- `route_position` (`number|null`, `0..1` around the route)

## Group: `transit_access_gap_semantics`

- `capture_context` (`transit_access_gap_capture_context`)
- `summary` (`transit_access_gap_summary`)
- `hotspots` (`array<transit_access_gap_hotspot>`)
- `source_component` (`string`)
- `metric_metadata` (`object<string, metric_definition>`)

### `transit_access_gap_capture_context`

- `capture_mode` (`string`)
- `capture_duration_seconds` (`number|null`, `seconds`)
- `recorded_trip_count` (`number|null`, `count`)
- `included_snapshot_count` (`number|null`, `count`)
- `outside_trip_mode` (`string`)
- `outside_trip_count` (`number|null`, `count`)
- `cluster_radius_m` (`number|null`, `meters`)
- `stop_coverage_radius_m` (`number|null`, `meters`)

### `transit_access_gap_summary`

- `hotspots_total` (`number|null`, `count`)
- `hotspots_with_uncovered_demand` (`number|null`, `count`)
- `high_priority_hotspots` (`number|null`, `count`)
- `critical_priority_hotspots` (`number|null`, `count`)

### `transit_access_gap_hotspot`

- `hotspot_id` (`string`)
- `label` (`string|null`)
- `center_position` (`position3`)
- `observed_trip_count` (`number|null`, `count`)
- `sample_route_count` (`number|null`, `count`)
- `bucket_index` (`number|null`)
- `priority_score` (`number|null`, `score`)
- `uncovered_share_percent` (`number|null`, `%`)
- `average_nearest_stop_distance_m` (`number|null`, `meters`)
- `average_uncovered_distance_m` (`number|null`, `meters`)
- `includes_outside_trips` (`bool`)
- `sample_routes` (`array<transit_access_gap_sample_route>`)

### `transit_access_gap_sample_route`

- `sample_index` (`number`)
- `segment_count` (`number`)
- `segments` (`array<transit_access_gap_route_segment>`)

### `transit_access_gap_route_segment`

- `path_target_entity_index` (`number`)
- `path_target_entity_version` (`number`)
- `is_forward` (`bool|null`)

## Nested Summaries

- `workforce_level_summary`
  - `level`, `total`, `total_percent`, `workers`, `unemployed`, `unemployed_percent`, `homeless`, `employable`, `outside`, `under`

- `workplace_level_summary`
  - `level`, `total`, `service`, `commercial`, `leisure`, `extractor`, `industrial`, `office`, `employees`, `open`, `commuter`

- `level_count_summary`
  - `level_0`, `level_1`, `level_2`, `level_3`, `level_4`, `total`

- `workforce_by_education_summary`
  - `potential` (`level_count_summary`)
  - `workers` (`level_count_summary`)
  - `unemployed` (`level_count_summary`)
  - `underemployed` (`level_count_summary`)

- `official_time_statistics`
  - `game_tick`, `game_year`, `game_month`, `game_day`, `days_per_year`, `sample_count`, `k_updates_per_day`, `k_ticks_per_day`

- `official_finance_statistics`
  - `money`, `income`, `expense`, `trade`

- `official_tax_statistics`
  - `residential_taxable_income`, `commercial_taxable_income`, `industrial_taxable_income`, `office_taxable_income`

- `official_population_flow_statistics`
  - `population`, `population_with_move_in`, `citizens_moved_in`, `citizens_moved_away`, `birth_rate`, `death_rate`

- `official_social_statistics`
  - `wellbeing`, `health`, `wellbeing_level`, `health_level`, `homeless_count`, `crime_rate`, `crime_count`, `escaped_arrest_count`, `collected_mail`, `delivered_mail`

- `official_tourism_statistics`
  - `tourist_count`, `tourist_income`, `lodging_used`, `lodging_total`, `current_tourists`, `average_tourists`, `attractiveness`

- `official_transport_totals_statistics`
  - `passenger_count_bus`, `passenger_count_subway`, `passenger_count_train`, `passenger_count_tram`, `passenger_count_airplane`, `passenger_count_taxi`, `passenger_count_ship`, `cargo_count_truck`, `cargo_count_train`, `cargo_count_ship`, `cargo_count_airplane`

- `official_sector_statistics`
  - `service`, `processing`, `office` (`official_sector_metric`)

- `official_sector_metric`
  - `wealth`, `count`, `workers`, `max_workers`

- `official_city_service_statistics`
  - `city_service_workers`, `city_service_max_workers`, `senior_worker_in_demand_percentage`, `dev_tree_points`

- `sector_int_summary`
  - `total`, `service`, `commercial`, `leisure`, `extractor`, `industrial`, `office`

- `sector_double_summary`
  - `total`, `service`, `commercial`, `leisure`, `extractor`, `industrial`, `office`

- `mode_entity_counts`
  - `bus`, `tram`, `subway`, `train`, `ship`, `ferry`, `air`, `taxi`, `unknown` (`number|null`)

- `mode_double_values`
  - `bus`, `tram`, `subway`, `train`, `ship`, `ferry`, `air`, `taxi`, `unknown` (`number|null`)

- `mobility_line_record`
  - `line_entity_index` (`number`)
  - `line_entity_version` (`number`)
  - `line_name` (`string|null`)
  - `line_identifier` (`string|null`) - XTM acronym when available, otherwise route number text
  - `line_identifier_source` (`string`) - `xtm_acronym|route_number|none`
  - `route_number` (`number|null`)
  - `mode` (`string`)
  - `is_cargo` (`boolean`)
  - `line_color` (`string|null`, `#RRGGBB`)
  - `active` (`boolean`)
  - `visible` (`boolean`)
  - `schedule` (`string`) - `day|night|day_and_night|unknown`
  - `stops` (`number`)
  - `active_vehicle_entities` (`number`)
  - `onboard_passenger_entities` (`number|null`) - current onboard riders aggregated across active vehicles mapped to the line
  - `total_passenger_capacity` (`number|null`) - total passenger capacity aggregated across active vehicles mapped to the line
  - `usage_percent` (`number|null`, `%`) - `onboard_passenger_entities / total_passenger_capacity * 100`, aligned to the in-game line usage panel when capacity data resolves
  - `length_m` (`number|null`)

- `metric_definition`
  - `measurement_kind`: `observed|derived|proxy`
  - `time_basis`: `instant|monthly|rolling|capture_window`
  - `units`: unit label for interpretation
  - `source_component`: ECS/system provenance

## Meta

- `source` (`string`) current value: `ecs_observed`
- `notes` (`string[]`)
- `metric_status` (`object<string,string>`) keyed by group name

## Interpretation Notes

- Mobility line records are read from runtime line UI/ECS state and are suitable for deterministic line-name and line-color export.
- `line_identifier` prefers XTM route acronym when present; fallback is route number.
- `usage_percent` is derived from observed onboard passengers and resolved vehicle passenger capacity; it should be preferred over vehicle-count heuristics when ranking route pressure.
- `official_city_statistics` mirrors aggregate city counters exposed by managed game systems. Use it as the official-panel-aligned aggregate layer, while keeping existing semantic groups for exporter-derived diagnostics and entity-level context.
- `land_value_*`, trade-value, and service-trade fields can remain `null` until stable ECS mappings are validated.
- `facility_identity` intentionally mixes direct building-family counts with active work-provider sector counts; provider-side counts are live facility meaning, not full citywide parcel inventory.
- `company_service_semantics` is provider-side and live. It summarizes active staffing pressure by sector and does not, by itself, prove profits, resource shortages, trade flow, or production output.
- `housing_pressure_semantics` is pressure-facing, not exact occupancy. It uses residential-building counts plus deduplicated household-side runtime counts and does not, by itself, prove exact occupied units or exact residential capacity.
- `household_pressure_context` is household-side runtime context. It is useful for interpreting local, homeless, moving-away, and property-linked household pressure, but it is still a citywide summary rather than a parcel-by-parcel diagnosis.
- `labor_pressure_context` combines live workforce and workplace totals for citywide pressure interpretation. It does not, by itself, prove exact hiring blockers or job-suitability causality.
- `transit_performance_semantics` is line-first and live. It improves understanding of operational pressure and weak service, but it does not by itself prove station-level crowding, transfer quality, or transport-building meaning.
- `transit_access_gap_semantics` is capture-window advisory evidence. It highlights observed passenger trip hotspots, nearby passenger-stop coverage gaps, and bounded sample routes, but it does not by itself prove latent demand or line-level causality.
- `transit_access_gap_semantics` should remain `unavailable` when no passenger-only runtime trip carrier is proven. Sample route segments are path-target references, not validated network edges, and `is_forward` can be `null` when direction is unresolved.
