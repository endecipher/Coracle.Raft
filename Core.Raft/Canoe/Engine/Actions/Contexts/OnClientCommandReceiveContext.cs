using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Canoe.Engine.States.LeaderState;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal sealed class OnClientCommandReceiveContextDependencies
    {
        internal IAppendEntriesManager AppendEntriesManager { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal IClientRequestHandler ClientRequestHandler { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnClientCommandReceiveContext<TCommand> : IActionContext where TCommand : class, ICommand
    {
        public OnClientCommandReceiveContext(IChangingState state, OnClientCommandReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal TCommand Command { get; set; }
        public DateTimeOffset InvocationTime { get; internal set; }
        public string UniqueCommandId { get; internal set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsNotStarted();
        internal IChangingState State { get; set; }
        OnClientCommandReceiveContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        public IClientRequestHandler ClientRequestHandler => Dependencies.ClientRequestHandler;
        public IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        public IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        public ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        public IPersistentProperties PersistentState => Dependencies.PersistentState;
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
