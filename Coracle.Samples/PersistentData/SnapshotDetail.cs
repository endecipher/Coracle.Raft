using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Samples.PersistentData
{
    public class SnapshotDetail : ISnapshotHeader
    {
        public string SnapshotId { get; set; }

        public long LastIncludedIndex { get; set; }

        public long LastIncludedTerm { get; set; }
    }
}
