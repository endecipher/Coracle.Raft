using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.States;
using System;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnSendRequestVoteRPCContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal IRemoteManager RemoteManager { get; set; }
        internal ICurrentStateAccessor CurrentStateAccessor { get; set; }
    }

    internal sealed class OnSendRequestVoteRPCContext : IActionContext
    {
        public OnSendRequestVoteRPCContext(INodeConfiguration toNode, long currentTerm, OnSendRequestVoteRPCContextDependencies deps)
        {
            Dependencies = deps;
            ElectionTerm = currentTerm;
            NodeConfiguration = toNode;
        }

        internal INodeConfiguration NodeConfiguration { get; }
        internal DateTimeOffset InvocationTime { get; set; }
        internal long ElectionTerm { get; }
        public bool IsContextValid => !State.IsDisposed && State.StateValue.IsCandidate();
        internal IChangingState State => CurrentStateAccessor.Get();
        OnSendRequestVoteRPCContextDependencies Dependencies { get; set; }


        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentProperties PersistentState => Dependencies.PersistentState;
        internal IRemoteManager RemoteManager => Dependencies.RemoteManager;
        internal ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
