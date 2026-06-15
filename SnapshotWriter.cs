using System;
using System.IO;
using System.Linq;
using System.Text;
using Colossal.Json;

namespace CS2DataExport
{
    public sealed class SnapshotWriteResult
    {
        public SnapshotWriteResult(string snapshotPath, string latestPath, int keptSnapshots, int deletedSnapshots)
        {
            SnapshotPath = snapshotPath;
            LatestPath = latestPath;
            KeptSnapshots = keptSnapshots;
            DeletedSnapshots = deletedSnapshots;
        }

        public string SnapshotPath { get; }
        public string LatestPath { get; }
        public int KeptSnapshots { get; }
        public int DeletedSnapshots { get; }
    }

    public sealed class SnapshotWriter
    {
        public SnapshotWriteResult WriteSnapshot(
            CitySnapshotV1 snapshot,
            DateTimeOffset exportedAtUtc,
            ExportSettings settings)
        {
            string outputRoot = settings.ResolveOutputRoot();
            string snapshotsDir = settings.ResolveSnapshotsDirectory();
            string latestPath = settings.ResolveLatestFilePath();

            Directory.CreateDirectory(outputRoot);
            Directory.CreateDirectory(snapshotsDir);

            string timestamp = exportedAtUtc.UtcDateTime.ToString("yyyyMMdd-HHmmss");
            string snapshotPath = Path.Combine(snapshotsDir, timestamp + ".json");

            string payload = JSON.Dump(snapshot);

            WriteTextAtomic(snapshotPath, payload);
            WriteTextAtomic(latestPath, payload);

            int deleted = ApplyRetentionPolicy(snapshotsDir, settings.EffectiveRetentionCount);
            int kept = Directory.GetFiles(snapshotsDir, "*.json", SearchOption.TopDirectoryOnly).Length;

            return new SnapshotWriteResult(snapshotPath, latestPath, kept, deleted);
        }

        private static void WriteTextAtomic(string filePath, string payload)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("Could not resolve directory for '" + filePath + "'.");
            }

            Directory.CreateDirectory(directory);
            string tempPath = Path.Combine(directory, Path.GetFileName(filePath) + ".tmp");

            File.WriteAllText(tempPath, payload, new UTF8Encoding(false));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(tempPath, filePath);
        }

        private static int ApplyRetentionPolicy(string snapshotsDir, int retentionCount)
        {
            string[] snapshots = Directory
                .GetFiles(snapshotsDir, "*.json", SearchOption.TopDirectoryOnly)
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
                .ToArray();

            if (snapshots.Length <= retentionCount)
            {
                return 0;
            }

            int deleted = 0;
            for (int i = retentionCount; i < snapshots.Length; i++)
            {
                File.Delete(snapshots[i]);
                deleted++;
            }

            return deleted;
        }
    }
}
