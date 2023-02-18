using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Samples.PersistentData
{
    public class DataChunk : ISnapshotChunkData
    {
        public byte[] Data { get; set; }
        public byte[] Config { get; set; }
    }
}
