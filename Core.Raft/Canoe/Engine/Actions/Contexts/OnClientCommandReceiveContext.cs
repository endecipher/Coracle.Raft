using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Responsibilities;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal class OnClientCommandReceiveContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal IResponsibilities Responsibilities { get; set; }
        internal IRemoteManager RemoteManager { get; set; }
        internal IClientRequestHandler ClientRequestHandler { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal class OnClientCommandReceiveContext<TCommand> : IActionContext where TCommand : class, ICommand
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
        public OnClientCommandReceiveContextDependencies Dependencies { get; }

        #region Action Dependencies
        public IResponsibilities Responsibilities => Dependencies.Responsibilities;
        public IClientRequestHandler ClientRequestHandler => Dependencies.ClientRequestHandler;
        public IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        public IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        public IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        public ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        public IPersistentProperties PersistentState => Dependencies.PersistentState;
        public IRemoteManager RemoteManager => Dependencies.RemoteManager;

        #endregion

        public void Dispose()
        {
            Command = null;
            State = null;
        }
    }
}
