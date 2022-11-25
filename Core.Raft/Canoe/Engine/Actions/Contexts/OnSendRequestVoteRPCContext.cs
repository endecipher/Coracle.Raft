using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Responsibilities;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
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
