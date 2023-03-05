#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Raft.Examples.Data
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
