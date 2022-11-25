using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.States;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal sealed class OnConfigurationChangeReceiveContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal IClusterConfigurationChanger ClusterConfigurationChanger { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnConfigurationChangeReceiveContext : IActionContext 
    {
        public OnConfigurationChangeReceiveContext(IChangingState state, OnConfigurationChangeReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal ConfigurationChangeRPC ConfigurationChange { get; set; }
        
        
        public DateTimeOffset InvocationTime { get; internal set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsNotStarted();
        internal IChangingState State { get; set; }


        OnConfigurationChangeReceiveContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IPersistentProperties PersistentState => Dependencies.PersistentState;
        internal IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        internal IClusterConfigurationChanger ClusterConfigurationChanger => Dependencies.ClusterConfigurationChanger;
        #endregion

        public void Dispose()
        {
            Dependencies = null;
            ConfigurationChange = null;
            State = null;
        }
    }
}
