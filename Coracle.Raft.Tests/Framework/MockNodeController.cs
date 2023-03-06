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

using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Tests.Components.Helper;

namespace Coracle.Raft.Tests.Framework
{
    public class MockNodeController
    {
        internal NodeConfiguration Configuration { get; init; }
        internal RemoteAwaitedLock<IAppendEntriesRPC, AppendEntriesRPCResponse> AppendEntriesLock { get; init; }
        internal RemoteAwaitedLock<IRequestVoteRPC, RequestVoteRPCResponse> RequestVoteLock { get; init; }
        internal RemoteAwaitedLock<IInstallSnapshotRPC, InstallSnapshotRPCResponse> InstallSnapshotLock { get; init; }

        public void EnqueueNextRequestVoteResponse(Func<IRequestVoteRPC, object> func, bool approveImmediately = false)
        {
            var @lock = RequestVoteLock.Enqueue(func);

            if (approveImmediately)
                @lock.Set();
        }

        public void EnqueueNextInstallSnapshotResponse(Func<IInstallSnapshotRPC, object> func, bool approveImmediately = false)
        {
            var @lock = InstallSnapshotLock.Enqueue(func);

            if (approveImmediately)
                @lock.Set();
        }

        public void ApproveNextRequestVoteInLine() => RequestVoteLock.ApproveNextInLine();

        public void EnqueueNextAppendEntriesResponse(Func<IAppendEntriesRPC, object> func, bool approveImmediately = false)
        {
            var @lock = AppendEntriesLock.Enqueue(func);

            if (approveImmediately)
                @lock.Set();
        }

        public void ApproveNextAppendEntriesInLine() => AppendEntriesLock.ApproveNextInLine();

        public void ClearQueues()
        {
            AppendEntriesLock.Clear();
            InstallSnapshotLock.Clear();
            RequestVoteLock.Clear();
        }
    }
}
