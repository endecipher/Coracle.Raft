using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Canoe.Engine.States.LeaderState;
using EventGuidance.Responsibilities;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal class OnSendAppendEntriesRPCHeartbeatContext : IAppendEntriesRPCContext
    {
        public OnSendAppendEntriesRPCHeartbeatContext(IChangingState state, OnSendAppendEntriesRPCContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        public INodeConfiguration NodeConfiguration { get; internal set; }
        public int CurrentRetryCounter { get; internal set; } = new();
        public DateTimeOffset InvocationTime { get; internal set; }
        public AppendEntriesSession Session { get; internal set; }
        public bool IsContextValid => !State.IsDisposed && State.StateValue.IsLeader();
        internal IChangingState State { get; set; }
        public OnSendAppendEntriesRPCContextDependencies Dependencies { get; }



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
