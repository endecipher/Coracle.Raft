using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnConfigurationChangeReceiveContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal IMembershipChanger ClusterConfigurationChanger { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnConfigurationChangeReceiveContext : IActionContext
    {
        public OnConfigurationChangeReceiveContext(IStateDevelopment state, OnConfigurationChangeReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal ConfigurationChangeRequest ConfigurationChange { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State { get; set; }
        OnConfigurationChangeReceiveContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        internal IMembershipChanger ClusterConfigurationChanger => Dependencies.ClusterConfigurationChanger;
        #endregion

        public void Dispose()
        {
            Dependencies = null;
            ConfigurationChange = null;
            State = null;
        }
    }
}
