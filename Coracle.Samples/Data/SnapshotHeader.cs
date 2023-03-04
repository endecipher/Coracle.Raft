using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Samples.Data
{
    public class SnapshotHeader : ISnapshotHeader
    {
        public string SnapshotId { get; set; }

        public long LastIncludedIndex { get; set; }

        public long LastIncludedTerm { get; set; }
    }
}
