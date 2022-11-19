using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting;
using EventGuidance.Responsibilities;
using System;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    internal class StateChanger : IStateChanger
    {
        #region Constants
        private const string OldStateValue = nameof(OldStateValue);
        private const string NewStateValue = nameof(NewStateValue);
        private const string StateChangerEntity = nameof(StateChanger);
        private const string FailedToChangeStateDueToInvalidRoute = nameof(FailedToChangeStateDueToInvalidRoute);

        #endregion

        #region Dependencies

        IActivityLogger ActivityLogger { get; }
        IPersistentProperties PersistentState { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        IClientRequestHandler ClientRequestHandler { get; }
        IElectionTimer ElectionTimer { get; }
        IResponsibilities Responsibilities { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IHeartbeatTimer HeartbeatTimer { get; }
        IRemoteManager RemoteManager { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }

        #endregion

        //TODO: From DI
        public StateChanger(
            IActivityLogger activityLogger, 
            IPersistentProperties persistentState,
            IGlobalAwaiter globalAwaiter,
            IClientRequestHandler clientRequestHandler,
            IElectionTimer electionTimer,
            IResponsibilities responsibilities,
            IClusterConfiguration clusterConfiguration,
            ILeaderNodePronouncer leaderNodePronouncer,
            IEngineConfiguration engineConfiguration,
            IHeartbeatTimer heartbeatTimer,
            IRemoteManager remoteManager,
            ICurrentStateAccessor currentStateAccessor
            )
        {
            ActivityLogger = activityLogger;
            PersistentState = persistentState;
            GlobalAwaiter = globalAwaiter;
            ClientRequestHandler = clientRequestHandler;
            ElectionTimer = electionTimer;
            Responsibilities = responsibilities;
            ClusterConfiguration = clusterConfiguration;
            LeaderNodePronouncer = leaderNodePronouncer;
            EngineConfiguration = engineConfiguration;
            HeartbeatTimer = heartbeatTimer;
            RemoteManager = remoteManager;
            CurrentStateAccessor = currentStateAccessor;
        }

        /// <summary>
        /// This encompasses creation and initialization of a new <see cref="Follower"/> state.
        /// 
        /// <see cref="ICanoeNode"/>'s state is changed to the new <see cref="Follower"/> state.
        /// </summary>
        public void Initialize()
        {
            var initialFollowerState = new Follower();

            FillFollowerDependencies(initialFollowerState);

            var volatileProperties = new VolatileProperties
            {
                CommitIndex = 0,
                LastApplied = 0
            };

            initialFollowerState.InitializeOnStateChange(volatileProperties).Wait();

            CurrentStateAccessor.UpdateWith(initialFollowerState);

            initialFollowerState.OnStateEstablishment().Wait();
        }

        public void AbandonStateAndConvertTo<T>(string typename) where T : IChangingState, new()
        {
            bool canContinue = false;
            Action<IChangingState> fillDependencies;
            var oldStateValue = CurrentStateAccessor.Get().StateValue;

            switch (typename)
            {
                case nameof(Leader):
                    canContinue = ValidateOldStateIn(oldStateValue, StateValues.Candidate);
                    fillDependencies = FillLeaderDependencies;
                    break;
                case nameof(Candidate):
                    canContinue = ValidateOldStateIn(oldStateValue, StateValues.Follower);
                    fillDependencies = FillCandidateDependencies;
                    break;
                case nameof(Follower):
                    canContinue = ValidateOldStateIn(oldStateValue, StateValues.Candidate, StateValues.Leader);
                    fillDependencies = FillFollowerDependencies;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid new state value {typename}");
            }

            if (!canContinue)
            {
                

                //TODO: I think we can log Exception and not do anything
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"State change from {oldStateValue} to {typename} failed",
                    EntitySubject = StateChangerEntity,
                    Event = FailedToChangeStateDueToInvalidRoute,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(OldStateValue, oldStateValue.ToString()))
                .With(ActivityParam.New(NewStateValue, typename))
                .WithCallerInfo());

                return;
            }

            var newState = new T();

            //Commented call to this, since ConfigureNew will be called internally at Abstract State level.
            //ComponentContainer.Instance.GetInstance<IResponsibilities>().ConfigureNew();

            var oldState = CurrentStateAccessor.Get();

            fillDependencies.Invoke(newState);

            newState.InitializeOnStateChange(oldState.VolatileState).Wait();
            
            CurrentStateAccessor.UpdateWith(newState);
            
            oldState.OnStateChangeBeginDisposal();

            newState.OnStateEstablishment().Wait();
        }

        private void FillLeaderDependencies(IChangingState newState)
        {
            newState.StateChanger = this;

            FillCommonDependencies(newState as IStateDependencies);

            var state = newState as ILeaderDependencies;

            state.LeaderNodePronouncer = LeaderNodePronouncer;
            state.LeaderProperties = new LeaderVolatileProperties(ActivityLogger, ClusterConfiguration, PersistentState);
            state.AppendEntriesMonitor = new LeaderState.AppendEntriesMonitor(ActivityLogger, ClusterConfiguration, EngineConfiguration, GlobalAwaiter);
            state.HeartbeatTimer = HeartbeatTimer;
            state.RemoteManager = RemoteManager;
        }

        private void FillCandidateDependencies(IChangingState newState)
        {
            newState.StateChanger = this;

            FillCommonDependencies(newState as IStateDependencies);

            var state = newState as ICandidateDependencies;

            state.ElectionSession = new ElectionSession(ActivityLogger, ClusterConfiguration, this);
            state.RemoteManager = RemoteManager;
        }

        private void FillFollowerDependencies(IChangingState newState)
        {
            newState.StateChanger = this;

            FillCommonDependencies(newState as IStateDependencies);
        }

        private void FillCommonDependencies(IStateDependencies state)
        {
            state.ActivityLogger = ActivityLogger;
            state.Responsibilities = Responsibilities;
            state.PersistentState = PersistentState;
            state.ClusterConfiguration = ClusterConfiguration;
            state.EngineConfiguration = EngineConfiguration;
            state.ElectionTimer = ElectionTimer;
            state.GlobalAwaiter = GlobalAwaiter;
            state.ClientRequestHandler = ClientRequestHandler;
        }

        #region Validations

        private bool ValidateOldStateIn(StateValues oldStateValue, params StateValues[] availableStates)
        {
            foreach (var state in availableStates)
            {
                if (oldStateValue == state)
                    return true;
            }

            return false;
        }

        #endregion
    }
}
