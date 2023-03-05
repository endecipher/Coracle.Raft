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

using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Raft.Examples.Data
{
    public interface ISnapshotManager
    {
        Task<ISnapshotHeader> CreateFile(IEnumerable<LogEntry> logEntries, IEnumerable<INodeConfiguration> currentNodeConfigurations);
        Task<ISnapshotHeader> CreateFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm);
        Task OnlyKeep(ISnapshotHeader compactedSnapshotHeader);
        Task<ISnapshotFile> GetFile(ISnapshotHeader detail);
        Task<ISnapshotFile> GetFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm);
        Task<(bool HasFile, ISnapshotHeader Detail)> HasFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm);
        Task<bool> HasFile(ISnapshotHeader detail);
        Task UpdateLastUsed(ISnapshotHeader snapshotDetail);
        Task<DateTimeOffset> GetLastUsed(ISnapshotHeader detail);
    }
}
