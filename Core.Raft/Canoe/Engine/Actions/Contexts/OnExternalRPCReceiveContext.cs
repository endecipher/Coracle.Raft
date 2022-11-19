using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal class OnExternalRPCReceiveContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentProperties PersistentState { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal class OnExternalRPCReceiveContext<T> : IActionContext where T : IRemoteCall
    {
        public OnExternalRPCReceiveContext(IChangingState state, OnExternalRPCReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal T Request { get; set; }
        internal IChangingState State { get; set; }
        public OnExternalRPCReceiveContextDependencies Dependencies { get; }

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsNotStarted();



        #region Action Dependencies
        public IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        public IPersistentProperties PersistentState => Dependencies.PersistentState;
        public ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;

        #endregion

        public void Dispose()
        {
            Request = default(T);
            State = null;
        }
    }
}
