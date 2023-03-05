using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnSendRequestVoteRPCContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal IOutboundRequestHandler RemoteManager { get; set; }
        internal ICurrentStateAccessor CurrentStateAccessor { get; set; }
    }

    internal sealed class OnSendRequestVoteRPCContext : IOutboundRpcContext
    {
        public OnSendRequestVoteRPCContext(INodeConfiguration toNode, long currentTerm, OnSendRequestVoteRPCContextDependencies deps)
        {
            Dependencies = deps;
            ElectionTerm = currentTerm;
            NodeConfiguration = toNode;
        }

        public INodeConfiguration NodeConfiguration { get; set; }
        internal long ElectionTerm { get; }
        public bool IsContextValid => !State.IsDisposed && State.StateValue.IsCandidate();
        internal IStateDevelopment State => CurrentStateAccessor.Get();
        OnSendRequestVoteRPCContextDependencies Dependencies { get; set; }


        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal IOutboundRequestHandler RemoteManager => Dependencies.RemoteManager;
        internal ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
