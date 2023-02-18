using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Samples.PersistentData
{
    public class File : ISnapshotFile
    {
        object _lock = new object();

        public const int MaxBytesInChunk = 100;
        public const int MaxChunkSize = 100;

        bool IsLockedForWrites = false;
        DataChunk[] DataChunks = new DataChunk[MaxChunkSize];
        int LastOffset { get; set; } = default;

        public Task Complete()
        {
            lock (_lock)
            {
                IsLockedForWrites = true;

                return Task.CompletedTask;
            }
        }

        public Task Fill(int offset, ISnapshotChunkData data)
        {
            lock (_lock)
            {
                if (IsLockedForWrites)
                    return Task.CompletedTask;

                LastOffset = Math.Max(offset, LastOffset);

                DataChunks[offset] = data as DataChunk;

                return Task.CompletedTask;
            }
        }

        public Task AppendConfiguration(byte[] configurationData)
        {
            lock (_lock)
            {
                if (IsLockedForWrites)
                    return Task.CompletedTask;

                DataChunks[LastOffset].Config = configurationData;

                return Task.CompletedTask;
            }
        }

        public Task<bool> HasOffset(int offset)
        {
            lock (_lock)
            {
                return Task.FromResult(offset >= 0 && offset <= MaxChunkSize - 1 && DataChunks[offset] != null);
            }
        }

        public Task<bool> IsLastOffset(int offset)
        {
            lock (_lock)
            {
                return Task.FromResult(offset == LastOffset);
            }
        }

        public async Task<ISnapshotChunkData> ReadDataAt(int offset)
        {
            if (!await HasOffset(offset))
                throw new ArgumentOutOfRangeException($"{nameof(offset)} is out of bounds or no chunk exists");

            lock (_lock)
            {
                return DataChunks[offset];
            }
        }

        public Task<byte[]> ReadData()
        {
            IEnumerable<byte> concatenatedArr = null; 

            lock (_lock)
            {
                for (int i = 0; i <= LastOffset; i++)
                {
                    if (concatenatedArr == null)
                        concatenatedArr = DataChunks[i].Data;
                    else
                        concatenatedArr = concatenatedArr.Concat(DataChunks[i].Data);
                }
            }

            return Task.FromResult(concatenatedArr.ToArray());
        }

        public Task<byte[]> ReadConfiguration()
        {
            lock (_lock)
            {
                for (int i = LastOffset; i >= 0; i--)
                {
                    var configuration = DataChunks[i].Config;

                    if (configuration != null)
                    {
                        return Task.FromResult<byte[]>(configuration);
                    }
                }
            }

            return Task.FromResult<byte[]>(null);
        }

        public async Task<bool> Verify(int lastOffSet)
        {
            if (!await HasOffset(lastOffSet))
                return false;

            lock (_lock)
            {
                bool isLastOffsetMatching = LastOffset == lastOffSet;
                bool areAllChunksFilledUpUntilLastOffset = true;

                for (int i = 0; i <= lastOffSet; i++)
                {
                    areAllChunksFilledUpUntilLastOffset &= DataChunks[i] != null;
                }

                return isLastOffsetMatching && areAllChunksFilledUpUntilLastOffset;
            }
        }

        public Task<int> GetLastOffset()
        {
            return Task.FromResult(LastOffset);
        }
    }
}
