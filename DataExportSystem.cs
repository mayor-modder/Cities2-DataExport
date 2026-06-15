using System;

namespace CS2DataExport;

public sealed record ExportTickResult(
    bool DidExport,
    string Reason,
    DateTimeOffset? NextDueUtc,
    string? SnapshotPath,
    int DeletedSnapshots,
    string? Error);

public interface ITransitAccessGapCaptureCoordinator
{
    void StartCaptureWindow(DateTimeOffset startedAtUtc, ExportSettings settings);
    void FinalizeCaptureWindow(DateTimeOffset finalizedAtUtc, ExportSettings settings);
    void ClearCompletedCapture();
}

public sealed class DataExportSystem
{
    private readonly ExportSettings _settings;
    private readonly MetricsCollector _collector;
    private readonly SnapshotWriter _writer;
    private readonly string _modVersion;
    private readonly string? _gameBuild;
    private readonly ITransitAccessGapCaptureCoordinator _transitAccessGapCaptureCoordinator;
    private readonly Action<string> _log;

    private DateTimeOffset? _nextDueUtc;
    private DateTimeOffset? _transitCaptureStartedAtUtc;
    private bool _transitCaptureWindowActive;
    private bool _transitCaptureWindowConsumed;

    public DataExportSystem(
        ExportSettings settings,
        MetricsCollector collector,
        SnapshotWriter writer,
        string modVersion,
        string? gameBuild,
        ITransitAccessGapCaptureCoordinator? transitAccessGapCaptureCoordinator = null,
        Action<string>? log = null)
    {
        _settings = settings;
        _collector = collector;
        _writer = writer;
        _modVersion = modVersion;
        _gameBuild = gameBuild;
        _transitAccessGapCaptureCoordinator = transitAccessGapCaptureCoordinator ?? new NoOpTransitAccessGapCaptureCoordinator();
        _log = log ?? (_ => { });
    }

    public ExportTickResult Tick(DateTimeOffset utcNow)
    {
        utcNow = utcNow.ToUniversalTime();

        if (!_settings.ExportEnabled)
        {
            return new ExportTickResult(
                DidExport: false,
                Reason: "disabled",
                NextDueUtc: _nextDueUtc,
                SnapshotPath: null,
                DeletedSnapshots: 0,
                Error: null);
        }

        _nextDueUtc ??= utcNow;

        if (utcNow < _nextDueUtc.Value)
        {
            return new ExportTickResult(
                DidExport: false,
                Reason: "not_due",
                NextDueUtc: _nextDueUtc,
                SnapshotPath: null,
                DeletedSnapshots: 0,
                Error: null);
        }

        try
        {
            bool finalizeCompletedCapture = ShouldFinalizeTransitCaptureWindow(utcNow);
            if (finalizeCompletedCapture)
            {
                _transitAccessGapCaptureCoordinator.FinalizeCaptureWindow(utcNow, _settings);
            }

            var snapshot = _collector.CollectSnapshot(utcNow, _modVersion, _gameBuild);
            var writeResult = _writer.WriteSnapshot(snapshot, utcNow, _settings);

            _nextDueUtc = AlignToNextDueBoundary(utcNow, _settings.EffectiveIntervalMinutes);

            if (finalizeCompletedCapture)
            {
                _transitAccessGapCaptureCoordinator.ClearCompletedCapture();
                _transitCaptureWindowActive = false;
                _transitCaptureStartedAtUtc = null;
            }

            if (ShouldStartTransitCaptureWindow())
            {
                _transitAccessGapCaptureCoordinator.StartCaptureWindow(utcNow, _settings);
                _transitCaptureWindowActive = true;
                _transitCaptureStartedAtUtc = utcNow;
                _transitCaptureWindowConsumed = true;
            }

            _log(
                $"export ok: snapshot='{writeResult.SnapshotPath}', latest='{writeResult.LatestPath}', " +
                $"kept={writeResult.KeptSnapshots}, deleted={writeResult.DeletedSnapshots}, next_due='{_nextDueUtc:O}'");

            return new ExportTickResult(
                DidExport: true,
                Reason: "exported",
                NextDueUtc: _nextDueUtc,
                SnapshotPath: writeResult.SnapshotPath,
                DeletedSnapshots: writeResult.DeletedSnapshots,
                Error: null);
        }
        catch (Exception ex)
        {
            _nextDueUtc = AlignToNextDueBoundary(utcNow, _settings.EffectiveIntervalMinutes);
            _log($"export failed: {ex}");

            return new ExportTickResult(
                DidExport: false,
                Reason: "error",
                NextDueUtc: _nextDueUtc,
                SnapshotPath: null,
                DeletedSnapshots: 0,
                Error: ex.Message);
        }
    }

    private static DateTimeOffset AlignToNextDueBoundary(DateTimeOffset utcNow, int intervalMinutes)
    {
        var nowMinutes = utcNow.ToUnixTimeSeconds() / 60;
        var nextWindowMinutes = ((nowMinutes / intervalMinutes) + 1) * intervalMinutes;
        return DateTimeOffset.FromUnixTimeSeconds(nextWindowMinutes * 60);
    }

    private bool ShouldStartTransitCaptureWindow()
    {
        return _settings.TransitTripCaptureMode == TransitTripCaptureMode.NextExportWindow
            && !_transitCaptureWindowActive
            && !_transitCaptureWindowConsumed;
    }

    private bool ShouldFinalizeTransitCaptureWindow(DateTimeOffset utcNow)
    {
        if (!_transitCaptureWindowActive || !_transitCaptureStartedAtUtc.HasValue)
        {
            return false;
        }

        double elapsedMinutes = (utcNow - _transitCaptureStartedAtUtc.Value).TotalMinutes;
        return elapsedMinutes >= _settings.EffectiveTransitTripCaptureWindowMinutes;
    }

    private sealed class NoOpTransitAccessGapCaptureCoordinator : ITransitAccessGapCaptureCoordinator
    {
        public void StartCaptureWindow(DateTimeOffset startedAtUtc, ExportSettings settings)
        {
        }

        public void FinalizeCaptureWindow(DateTimeOffset finalizedAtUtc, ExportSettings settings)
        {
        }

        public void ClearCompletedCapture()
        {
        }
    }
}
