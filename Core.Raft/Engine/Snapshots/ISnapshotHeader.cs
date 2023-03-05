namespace Coracle.Raft.Engine.Snapshots
{
    public interface ISnapshotHeader
    {
        /// <summary>
        /// Unique Snapshot Id for external reference
        /// </summary>
        public string SnapshotId { get; }

        /// <summary>
        /// Last logEntry index of the merged entries which this snapshot represents
        /// </summary>
        public long LastIncludedIndex { get; }

        /// <summary>
        /// Last logEntry's term of the merged entries which this snapshot represents
        /// </summary>
        public long LastIncludedTerm { get; }
    }
}
