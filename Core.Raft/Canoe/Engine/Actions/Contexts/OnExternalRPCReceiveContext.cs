using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
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

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsNotStarted();



        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentProperties PersistentState => Dependencies.PersistentState;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IClusterConfigurationChanger ClusterConfigurationChanger => Dependencies.ClusterConfigurationChanger;

        #endregion

        public void Dispose()
        {
            Request = default(T);
            State = null;
            Dependencies = null;
        }
    }
}
