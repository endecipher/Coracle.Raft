
using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Actions.Core
{

    public interface ISnapshotHeader
    {
        /// <summary>
        /// Snapshot Id for external reference
        /// </summary>
        public string SnapshotId { get; }

        public long LastIncludedIndex { get; }

        public long LastIncludedTerm { get; }
    }

    public interface ISnapshotFile
    {
        Task AppendConfiguration(byte[] configurationData);
        Task Complete();
        Task Fill(int offset, ISnapshotChunkData data);
        Task<bool> HasOffset(int offset);
        Task<bool> IsLastOffset(int offset);
        Task<byte[]> ReadConfiguration();
        Task<int> GetLastOffset();
        Task<byte[]> ReadData();
        Task<ISnapshotChunkData> ReadDataAt(int offset);
        Task<bool> Verify(int lastOffSet);
    }
}
