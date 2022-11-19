using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Responsibilities;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal class OnSendRequestVoteRPCContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal IRemoteManager RemoteManager { get; set; }
        internal IResponsibilities Responsibilities { get; set; }
    }

    internal class OnSendRequestVoteRPCContext : IActionContext
    {
        public OnSendRequestVoteRPCContext(IChangingState state, OnSendRequestVoteRPCContextDependencies deps)
        {
            State = state;
            Dependencies = deps;
        }

        public INodeConfiguration NodeConfiguration { get; internal set; }
        public int CurrentRetryCounter { get; internal set; } = new();
        public DateTimeOffset InvocationTime { get; internal set; }
        internal Guid SessionGuid { get; set; }
        public bool IsContextValid => !State.IsDisposed && State.StateValue.IsCandidate();
        internal IChangingState State { get; set; }
        public OnSendRequestVoteRPCContextDependencies Dependencies { get; }




        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentProperties PersistentState => Dependencies.PersistentState;
        internal IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        internal IRemoteManager RemoteManager => Dependencies.RemoteManager;
        internal IResponsibilities Responsibilities => Dependencies.Responsibilities;

        #endregion


        public void Dispose()
        {
            State = null;
        }
    }
}
