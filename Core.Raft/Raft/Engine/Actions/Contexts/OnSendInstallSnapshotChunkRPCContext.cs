using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Snapshots;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnSendInstallSnapshotChunkRPCContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal IOutboundRequestHandler RemoteManager { get; set; }
    }

    internal sealed class OnSendInstallSnapshotChunkRPCContext : IOutboundRpcContext
    {
        public OnSendInstallSnapshotChunkRPCContext(IStateDevelopment changingState, OnSendInstallSnapshotChunkRPCContextDependencies dependencies)
        {
            State = changingState;
            Dependencies = dependencies;
        }

        public INodeConfiguration NodeConfiguration { get; set; }
        internal int Offset { get; set; }
        internal ISnapshotHeader SnapshotHeader { get; set; }
        internal IStateDevelopment State { get; set; }
        OnSendInstallSnapshotChunkRPCContextDependencies Dependencies { get; set; }
        public bool IsContextValid => !State.IsDisposed && State.StateValue.IsLeader();

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal IOutboundRequestHandler RemoteManager => Dependencies.RemoteManager;


        #endregion

        public void Dispose()
        {
            State = null;
            Dependencies = null;
        }
    }
}
