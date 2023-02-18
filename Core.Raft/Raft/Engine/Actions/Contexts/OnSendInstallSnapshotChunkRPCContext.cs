using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using System;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnSendInstallSnapshotChunkRPCContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal IRemoteManager RemoteManager { get; set; }
    }

    internal sealed class OnSendInstallSnapshotChunkRPCContext : IAppendEntriesRPCContext
    {
        public OnSendInstallSnapshotChunkRPCContext(IChangingState changingState, OnSendInstallSnapshotChunkRPCContextDependencies dependencies)
        {
            State = changingState;
            Dependencies = dependencies;
        }

        public INodeConfiguration NodeConfiguration { get; internal set; }
        public DateTimeOffset InvocationTime { get; internal set; }
        public int Offset { get; internal set; }
        public ISnapshotHeader SnapshotHeader { get; internal set; }


        internal IChangingState State { get; set; }
        OnSendInstallSnapshotChunkRPCContextDependencies Dependencies { get; set; }
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
