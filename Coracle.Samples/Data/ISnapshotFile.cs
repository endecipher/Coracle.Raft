using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Raft.Examples.Data
{
    public interface ISnapshotFile
    {
        Task AppendConfiguration(byte[] configurationData);
        Task Complete();
        Task Fill(int offset, ISnapshotDataChunk data);
        Task<bool> HasOffset(int offset);
        Task<bool> IsLastOffset(int offset);
        Task<byte[]> ReadConfiguration();
        Task<int> GetLastOffset();
        Task<byte[]> ReadData();
        Task<ISnapshotDataChunk> ReadDataAt(int offset);
        Task<bool> Verify(int lastOffSet);
    }
}
