using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnSendAppendEntriesRPCContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal IOutboundRequestHandler RemoteManager { get; set; }
    }

    internal sealed class OnSendAppendEntriesRPCLogsContext : IOutboundRpcContext
    {
        public OnSendAppendEntriesRPCLogsContext(IStateDevelopment changingState, OnSendAppendEntriesRPCContextDependencies dependencies)
        {
            State = changingState;
            Dependencies = dependencies;
        }

        public INodeConfiguration NodeConfiguration { get; set; }
        internal IStateDevelopment State { get; set; }
        OnSendAppendEntriesRPCContextDependencies Dependencies { get; set; }
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
