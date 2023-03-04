using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Samples.Data
{
    public class SnapshotFile : ISnapshotFile
    {
        object _lock = new object();

        public const int MaxBytesInChunk = 100;
        public const int MaxChunkSize = 100;

        bool IsLockedForWrites = false;
        SnapshotDataChunk[] DataChunks = new SnapshotDataChunk[MaxChunkSize];
        int LastOffset { get; set; } = default;

        public Task Complete()
        {
            lock (_lock)
            {
                IsLockedForWrites = true;

                return Task.CompletedTask;
            }
        }

        public Task Fill(int offset, ISnapshotDataChunk data)
        {
            lock (_lock)
            {
                if (IsLockedForWrites)
                    return Task.CompletedTask;

                LastOffset = Math.Max(offset, LastOffset);

                DataChunks[offset] = data as SnapshotDataChunk;

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

        public async Task<ISnapshotDataChunk> ReadDataAt(int offset)
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
                        return Task.FromResult(configuration);
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
