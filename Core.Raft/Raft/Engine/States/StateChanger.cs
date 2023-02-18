using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using Coracle.Raft.Engine.States.LeaderEntities;

namespace Coracle.Raft.Engine.States
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
        IAppendEntriesManager AppendEntriesManager { get; }
        IClusterConfigurationChanger ClusterConfigurationChanger { get; }
        IElectionManager ElectionManager { get; }
        ILeaderVolatileProperties LeaderVolatileProperties { get; }

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
            ICurrentStateAccessor currentStateAccessor,
            IAppendEntriesManager appendEntriesManager,
            IClusterConfigurationChanger clusterConfigurationChanger,
            IElectionManager electionManager,
            ILeaderVolatileProperties leaderVolatileProperties
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
            AppendEntriesManager = appendEntriesManager;
            ClusterConfigurationChanger = clusterConfigurationChanger;
            ElectionManager = electionManager;
            LeaderVolatileProperties = leaderVolatileProperties;
        }

        private static object _lock = new object();

        /// <summary>
        /// This encompasses creation and initialization of a new <see cref="Follower"/> state.
        /// 
        /// <see cref="ICanoeNode"/>'s state is changed to the new <see cref="Follower"/> state.
        /// </summary>
        public void Initialize()
        {
            var initialFollowerState = new Follower();

            FillFollowerDependencies(initialFollowerState);

            var volatileProperties = new VolatileProperties(ActivityLogger)
            {
                CommitIndex = 0,
                LastApplied = 0
            };

            lock (_lock)
            {

                initialFollowerState.InitializeOnStateChange(volatileProperties).Wait();

                CurrentStateAccessor.UpdateWith(initialFollowerState);

                initialFollowerState.OnStateEstablishment().Wait();
            }
        }

        public void AbandonStateAndConvertTo<T>(string typename) where T : IChangingState, new()
        {
            lock (_lock)
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
        }

        private void FillLeaderDependencies(IChangingState newState)
        {
            newState.StateChanger = this;

            FillCommonDependencies(newState as IStateDependencies);

            var state = newState as ILeaderDependencies;

            state.LeaderProperties = LeaderVolatileProperties;
            state.AppendEntriesManager = AppendEntriesManager;
            state.HeartbeatTimer = HeartbeatTimer;
        }

        private void FillCandidateDependencies(IChangingState newState)
        {
            newState.StateChanger = this;

            FillCommonDependencies(newState as IStateDependencies);

            var state = newState as ICandidateDependencies;

            state.ElectionManager = ElectionManager;
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
            state.LeaderNodePronouncer = LeaderNodePronouncer;
            state.EngineConfiguration = EngineConfiguration;
            state.ElectionTimer = ElectionTimer;
            state.GlobalAwaiter = GlobalAwaiter;
            state.ClientRequestHandler = ClientRequestHandler;
            state.ClusterConfigurationChanger = ClusterConfigurationChanger;
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
