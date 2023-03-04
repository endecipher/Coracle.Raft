using Coracle.IntegrationTests.Components.Helper;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;
using System.Collections.Concurrent;

namespace Coracle.IntegrationTests.Framework
{
    public interface IMockNodeContext
    {
        void CreateMockNode(string nodeId);

        MockNodeController GetMockNode(string nodeId);
    }

    public class MockNodeContext : IMockNodeContext
    {
        ConcurrentDictionary<string, MockNodeController> MockNodes { get; set; } = new ConcurrentDictionary<string, MockNodeController>();

        public void CreateMockNode(string nodeId)
        {
            MockNodes.AddOrUpdate(nodeId, new MockNodeController
            {
                Configuration = new NodeConfiguration
                {
                    UniqueNodeId = nodeId,
                },
                AppendEntriesLock = new RemoteAwaitedLock<IAppendEntriesRPC, AppendEntriesRPCResponse>(),
                RequestVoteLock = new RemoteAwaitedLock<IRequestVoteRPC, RequestVoteRPCResponse>(),
                InstallSnapshotLock = new RemoteAwaitedLock<IInstallSnapshotRPC, InstallSnapshotRPCResponse>(),
            },
            (key, oldConfig) => oldConfig);
        }

        public MockNodeController GetMockNode(string nodeId)
        {
            bool isFound = MockNodes.TryGetValue(nodeId, out var node);

            if (isFound) { return node; } else throw new InvalidOperationException();
        }
    }
}
