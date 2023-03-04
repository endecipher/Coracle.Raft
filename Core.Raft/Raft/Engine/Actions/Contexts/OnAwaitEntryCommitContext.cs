using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{

    internal sealed class OnAwaitEntryCommitContextDependencies
    {
        public ICurrentStateAccessor CurrentStateAccessor { get; set; }
        public IPersistentStateHandler PersistentState { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }
    }


    internal sealed class OnAwaitEntryCommitContext : IActionContext
    {
        public OnAwaitEntryCommitContext(long logEntryIndex, OnAwaitEntryCommitContextDependencies dependencies)
        {
            LogEntryIndex = logEntryIndex;
            Dependencies = dependencies;
        }

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal long LogEntryIndex { get; }
        internal IStateDevelopment State => CurrentStateAccessor.Get();
        OnAwaitEntryCommitContextDependencies Dependencies { get; set; }

        #region Action Dependencies

        internal ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
