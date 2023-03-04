using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Samples.Data
{
    public class SnapshotDataChunk : ISnapshotDataChunk
    {
        public byte[] Data { get; set; }
        public byte[] Config { get; set; }
    }
}
