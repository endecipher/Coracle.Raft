using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnExternalRPCReceiveContextDependencies
    {
        internal IMembershipChanger ClusterConfigurationChanger { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnExternalRPCReceiveContext<T> : IActionContext where T : IRemoteCall
    {
        public OnExternalRPCReceiveContext(IStateDevelopment state, OnExternalRPCReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal T Request { get; set; }
        internal IStateDevelopment State { get; set; }
        OnExternalRPCReceiveContextDependencies Dependencies { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal bool TurnToFollower { get; set; } = false;

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IMembershipChanger ClusterConfigurationChanger => Dependencies.ClusterConfigurationChanger;

        #endregion

        public void Dispose()
        {
            Request = default;
            State = null;
            Dependencies = null;
        }
    }
}
