using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.States;
using System;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnSendAppendEntriesRPCContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal IRemoteManager RemoteManager { get; set; }
    }

    internal sealed class OnSendAppendEntriesRPCLogsContext : IAppendEntriesRPCContext
    {
        public OnSendAppendEntriesRPCLogsContext(IChangingState changingState, OnSendAppendEntriesRPCContextDependencies dependencies)
        {
            State = changingState;
            Dependencies = dependencies;
        }


        public INodeConfiguration NodeConfiguration { get; internal set; }
        public DateTimeOffset InvocationTime { get; internal set; }


        internal IChangingState State { get; set; }
        OnSendAppendEntriesRPCContextDependencies Dependencies { get; set; }

        internal bool TurnToFollower { get; set; } = false;
        public bool IsContextValid => !State.IsDisposed && State.StateValue.IsLeader();

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentProperties PersistentState => Dependencies.PersistentState;
        internal IRemoteManager RemoteManager => Dependencies.RemoteManager;
        #endregion

        public void Dispose()
        {
            State = null;
            Dependencies = null;
        }
    }
}
