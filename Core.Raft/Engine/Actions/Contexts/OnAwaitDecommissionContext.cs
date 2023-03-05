using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnAwaitDecommissionContextDependencies
    {
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal ICurrentStateAccessor CurrentStateAccessor { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
    }

    internal sealed class OnAwaitDecommissionContext : IActionContext
    {
        public OnAwaitDecommissionContext(OnAwaitDecommissionContextDependencies dependencies) : base()
        {
            Dependencies = dependencies;
        }

        internal long ConfigurationLogEntryIndex { get; set; }
        internal IEnumerable<NodeConfiguration> NewConfiguration { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State => CurrentStateAccessor.Get();
        OnAwaitDecommissionContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        public IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        public IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        public ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;
        public IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;

        #endregion
    }
}
