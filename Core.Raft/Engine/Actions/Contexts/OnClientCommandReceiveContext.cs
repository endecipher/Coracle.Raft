using Coracle.Raft.Engine.Command;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnClientCommandReceiveContextDependencies
    {
        internal IAppendEntriesManager AppendEntriesManager { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal IStateMachineHandler ClientRequestHandler { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnClientCommandReceiveContext<TCommand> : IActionContext where TCommand : class, ICommand
    {
        public OnClientCommandReceiveContext(IStateDevelopment state, OnClientCommandReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal TCommand Command { get; set; }
        internal string UniqueCommandId { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State { get; set; }
        OnClientCommandReceiveContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        public IStateMachineHandler ClientRequestHandler => Dependencies.ClientRequestHandler;
        public IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        public IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        public ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        public IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        public IAppendEntriesManager AppendEntriesManager => Dependencies.AppendEntriesManager;

        #endregion

        public void Dispose()
        {
            Command = null;
            State = null;
            Dependencies = null;
        }
    }
}
