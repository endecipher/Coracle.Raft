using Coracle.IntegrationTests.Components.Helper;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;

namespace Coracle.IntegrationTests.Framework
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
    }

    
}
