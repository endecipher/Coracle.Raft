
namespace Coracle.Raft.Engine.Actions.Core
{
    public interface ISnapshotChunkData
    {
        byte[] Data { get; }
    }
}
