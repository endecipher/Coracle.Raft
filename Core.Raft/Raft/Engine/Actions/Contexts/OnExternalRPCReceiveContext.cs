using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnExternalRPCReceiveContextDependencies
    {
        internal IClusterConfigurationChanger ClusterConfigurationChanger { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnExternalRPCReceiveContext<T> : IActionContext where T : IRemoteCall
    {
        public OnExternalRPCReceiveContext(IChangingState state, OnExternalRPCReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal T Request { get; set; }
        internal IChangingState State { get; set; }
        OnExternalRPCReceiveContextDependencies Dependencies { get; set; }

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal bool TurnToFollower { get; set; } = false;

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentProperties PersistentState => Dependencies.PersistentState;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IClusterConfigurationChanger ClusterConfigurationChanger => Dependencies.ClusterConfigurationChanger;

        #endregion

        public void Dispose()
        {
            Request = default;
            State = null;
            Dependencies = null;
        }
    }
}
