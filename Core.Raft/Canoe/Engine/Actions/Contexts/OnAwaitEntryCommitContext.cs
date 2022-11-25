using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.States;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{

    internal sealed class OnAwaitEntryCommitContextDependencies
    {
        public ICurrentStateAccessor CurrentStateAccessor { get; set; }
        public IPersistentProperties PersistentState { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }
    }


    internal sealed class OnAwaitEntryCommitContext : IActionContext
    {
        public OnAwaitEntryCommitContext(long logEntryIndex, OnAwaitEntryCommitContextDependencies dependencies)
        {
            LogEntryIndex = logEntryIndex;
            Dependencies = dependencies;
        }

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsNotStarted();

        internal long LogEntryIndex { get; }
        internal IChangingState State { get; set; }

        internal DateTimeOffset InvocationTime { get; set; }

        OnAwaitEntryCommitContextDependencies Dependencies { get; set; }

        #region Action Dependencies

        internal ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentProperties PersistentState => Dependencies.PersistentState;

        #endregion

        public void Dispose()
        {
            State = null;
            Dependencies = null;
        }
    }
}
