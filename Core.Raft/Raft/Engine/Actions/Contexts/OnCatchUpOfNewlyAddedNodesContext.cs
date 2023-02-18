using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using System;
using TaskGuidance.BackgroundProcessing.Core;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnCatchUpOfNewlyAddedNodesContextDependencies
    {
        public ICurrentStateAccessor CurrentStateAccessor { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }
    }

    internal sealed class OnCatchUpOfNewlyAddedNodesContext : IActionContext
    {
        public OnCatchUpOfNewlyAddedNodesContext(long logEntryIndex, string[] nodesToCheck, OnCatchUpOfNewlyAddedNodesContextDependencies dependencies)
        {
            LogEntryIndex = logEntryIndex;
            NodesToCheck = nodesToCheck;
            Dependencies = dependencies;
        }

        public bool IsContextValid
        {
            get
            {
                var state = CurrentStateAccessor.Get();
                return !state.IsDisposed && !state.StateValue.IsAbandoned() && !state.StateValue.IsStopped();
            }
        }

        internal string[] NodesToCheck { get; }
        internal long LogEntryIndex { get; }
        internal DateTimeOffset InvocationTime { get; set; }

        OnCatchUpOfNewlyAddedNodesContextDependencies Dependencies { get; set; }

        #region Action Dependencies

        internal ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }



    internal sealed class OnCompactionContextDependencies
    {
        public IEngineConfiguration EngineConfiguration { get; set; }
        public IPersistentProperties PersistentState { get; set; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
        public IResponsibilities Responsibilities { get; set; }
    }

    internal sealed class OnCompactionContext : IActionContext
    {
        public OnCompactionContext(OnCompactionContextDependencies dependencies, IChangingState state)
        {
            Dependencies = dependencies;
            State = state;
        }

        public bool IsContextValid
        {
            get
            {
                return !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
            }
        }

        internal DateTimeOffset InvocationTime { get; set; }
        public IChangingState State { get; }
        OnCompactionContextDependencies Dependencies { get; set; }

        #region Action Dependencies

        internal IPersistentProperties PersistentState => Dependencies.PersistentState;
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IResponsibilities Responsibilities => Dependencies.Responsibilities;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
