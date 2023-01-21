using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.States;
using System;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnAwaitDecomissionContextDependencies
    {
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal ICurrentStateAccessor CurrentStateAccessor { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
    }

    internal sealed class OnAwaitDecomissionContext : IActionContext
    {
        public OnAwaitDecomissionContext(OnAwaitDecomissionContextDependencies dependencies) : base()
        {
            Dependencies = dependencies;
        }

        internal long ConfigurationLogEntryIndex { get; set; }
        public DateTimeOffset InvocationTime { get; internal set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsNotStarted();
        internal IChangingState State => CurrentStateAccessor.Get();
        OnAwaitDecomissionContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        public IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        public IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        public ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
