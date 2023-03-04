using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Samples.Data
{
    public interface ISnapshotManager
    {
        Task<ISnapshotHeader> CreateFile(IEnumerable<LogEntry> logEntries, IEnumerable<INodeConfiguration> currentNodeConfigurations);
        Task<ISnapshotHeader> CreateFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm);
        Task OnlyKeep(ISnapshotHeader compactedSnapshotHeader);
        Task<ISnapshotFile> GetFile(ISnapshotHeader detail);
        Task<ISnapshotFile> GetFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm);
        Task<(bool HasFile, ISnapshotHeader Detail)> HasFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm);
        Task<bool> HasFile(ISnapshotHeader detail);
        Task UpdateLastUsed(ISnapshotHeader snapshotDetail);
        Task<DateTimeOffset> GetLastUsed(ISnapshotHeader detail);
    }
}
