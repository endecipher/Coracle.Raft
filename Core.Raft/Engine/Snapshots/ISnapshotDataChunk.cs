namespace Coracle.Raft.Engine.Snapshots
{
    public interface ISnapshotDataChunk
    {
        /// <summary>
        /// Data which represents a chunk of the entire state pointed by a <see cref="Snapshots.ISnapshotHeader"/> 
        /// </summary>
        byte[] Data { get; }
    }
}
