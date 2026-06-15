using System;
using Colossal.Logging;
using Unity.Entities;

namespace CS2DataExport
{
    public sealed partial class Mod
    {
        private static ILog? s_log;

        private ExportSettings? _settings;
        private DataExportSystem? _dataExportSystem;
        private TransitAccessGapCaptureCoordinator? _transitAccessGapCaptureCoordinator;
        private TransitAccessGapRuntimeObserver? _transitAccessGapRuntimeObserver;
        private EntityManager? _entityManager;
        private World? _world;
        private bool _initialized;

        public Mod()
        {
            // Keep constructor empty; initialize lazily in OnLoad/OnUpdate.
            // This is safer with CS2 mod loader behavior.
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _settings = new ExportSettings();
            _transitAccessGapCaptureCoordinator = new TransitAccessGapCaptureCoordinator(new TransitAccessGapAnalyzer());
            _transitAccessGapRuntimeObserver = new TransitAccessGapRuntimeObserver(_transitAccessGapCaptureCoordinator);
            _dataExportSystem = new DataExportSystem(
                settings: _settings,
                collector: new MetricsCollector(
                    new RuntimeEcsMetricProbe(
                        getEntityManager: () => _entityManager,
                        getWorld: () => _world,
                        log: SafeLog,
                        transitAccessGapCaptureCoordinator: _transitAccessGapCaptureCoordinator)),
                writer: new SnapshotWriter(),
                modVersion: "1.0.0",
                gameBuild: null,
                transitAccessGapCaptureCoordinator: _transitAccessGapCaptureCoordinator,
                log: SafeLog);

            _initialized = true;
        }

        public void OnLoad()
        {
            EnsureInitialized();

            if (_settings == null)
            {
                SafeLog("load failed: settings were not initialized.");
                return;
            }

            SafeLog(
                "loaded: enabled=" + _settings.ExportEnabled +
                ", interval_min=" + _settings.EffectiveIntervalMinutes +
                ", retention=" + _settings.EffectiveRetentionCount +
                ", output='" + _settings.ResolveOutputRoot() + "'");
        }

        public ExportTickResult OnUpdate(DateTimeOffset utcNow)
        {
            EnsureInitialized();

            if (_dataExportSystem == null)
            {
                return new ExportTickResult(
                    DidExport: false,
                    Reason: "error",
                    NextDueUtc: null,
                    SnapshotPath: null,
                    DeletedSnapshots: 0,
                    Error: "DataExportSystem is not initialized.");
            }

            if (_entityManager.HasValue && _settings != null && _transitAccessGapRuntimeObserver != null)
            {
                _transitAccessGapRuntimeObserver.Observe(_entityManager.Value, _settings);
            }

            return _dataExportSystem.Tick(utcNow);
        }

        public void OnDispose()
        {
            SafeLog("disposed");

            _entityManager = null;
            _world = null;
            _settings = null;
            _dataExportSystem = null;
            _transitAccessGapCaptureCoordinator = null;
            _transitAccessGapRuntimeObserver = null;
            _initialized = false;
        }

        internal void SetRuntimeContext(EntityManager entityManager, World world)
        {
            _entityManager = entityManager;
            _world = world;
        }

        private static void SafeLog(string message)
        {
            try
            {
                if (s_log == null)
                {
                    s_log = LogManager.GetLogger("CS2DataExport").SetShowsErrorsInUI(true);
                }

                if (s_log != null)
                {
                    s_log.Info(message);
                    return;
                }
            }
            catch
            {
                // Fallback below.
            }

            Console.WriteLine("[CS2DataExport] " + message);
        }
    }
}
