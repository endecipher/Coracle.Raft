using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnCompactionContextDependencies
    {
        public IEngineConfiguration EngineConfiguration { get; set; }
        public IPersistentStateHandler PersistentState { get; set; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
        public IResponsibilities Responsibilities { get; set; }
        public IClusterConfiguration ClusterConfiguration { get; set; }
    }

    internal sealed class OnCompactionContext : IActionContext
    {
        public OnCompactionContext(OnCompactionContextDependencies dependencies, IStateDevelopment state)
        {
            Dependencies = dependencies;
            State = state;
        }

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State { get; }
        OnCompactionContextDependencies Dependencies { get; set; }

        #region Action Dependencies

        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IResponsibilities Responsibilities => Dependencies.Responsibilities;
        internal IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
