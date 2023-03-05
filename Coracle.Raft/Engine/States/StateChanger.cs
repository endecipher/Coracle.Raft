#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using Coracle.Raft.Engine.States.LeaderEntities;
using Coracle.Raft.Engine.Actions;
using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Command;
using Coracle.Raft.Engine.Exceptions;

namespace Coracle.Raft.Engine.States
{
    internal class StateChanger : IStateChanger
    {
        #region Constants
        public const string oldStateValue = nameof(oldStateValue);
        public const string newStateValue = nameof(newStateValue);
        public const string StateChangerEntity = nameof(StateChanger);
        public const string FailedToChangeStateDueToInvalidRoute = nameof(FailedToChangeStateDueToInvalidRoute);

        #endregion

        #region Dependencies

        IActivityLogger ActivityLogger { get; }
        IPersistentStateHandler PersistentState { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        IStateMachineHandler ClientRequestHandler { get; }
        IElectionTimer ElectionTimer { get; }
        IResponsibilities Responsibilities { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IHeartbeatTimer HeartbeatTimer { get; }
        IOutboundRequestHandler RemoteManager { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IAppendEntriesManager AppendEntriesManager { get; }
        IMembershipChanger ClusterConfigurationChanger { get; }
        IElectionManager ElectionManager { get; }
        ILeaderVolatileProperties LeaderVolatileProperties { get; }

        #endregion

        public StateChanger(
            IActivityLogger activityLogger,
            IPersistentStateHandler persistentState,
            IGlobalAwaiter globalAwaiter,
            IStateMachineHandler clientRequestHandler,
            IElectionTimer electionTimer,
            IResponsibilities responsibilities,
            IClusterConfiguration clusterConfiguration,
            ILeaderNodePronouncer leaderNodePronouncer,
            IEngineConfiguration engineConfiguration,
            IHeartbeatTimer heartbeatTimer,
            IOutboundRequestHandler remoteManager,
            ICurrentStateAccessor currentStateAccessor,
            IAppendEntriesManager appendEntriesManager,
            IMembershipChanger clusterConfigurationChanger,
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
        /// <see cref="ICoracleNode"/>'s state is changed to the new <see cref="Follower"/> state.
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

        public void AbandonStateAndConvertTo<T>(string typename) where T : IStateDevelopment, new()
        {
            lock (_lock)
            {
                bool canContinue = false;
                Action<IStateDevelopment> fillDependencies;
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
                        throw InvalidStateConversionException.New(typename);
                }

                if (!canContinue)
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"State change from {oldStateValue} to {typename} failed",
                        EntitySubject = StateChangerEntity,
                        Event = FailedToChangeStateDueToInvalidRoute,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(StateChanger.oldStateValue, oldStateValue.ToString()))
                    .With(ActivityParam.New(newStateValue, typename))
                    .WithCallerInfo());

                    return;
                }


                var newState = new T();

                var oldState = CurrentStateAccessor.Get();

                fillDependencies.Invoke(newState);

                newState.InitializeOnStateChange(oldState.VolatileState).Wait();

                CurrentStateAccessor.UpdateWith(newState);

                oldState.OnStateChangeBeginDisposal();

                newState.OnStateEstablishment().Wait();
            }
        }

        private void FillLeaderDependencies(IStateDevelopment newState)
        {
            newState.StateChanger = this;

            FillCommonDependencies(newState as IStateDependencies);

            var state = newState as ILeaderDependencies;

            state.LeaderProperties = LeaderVolatileProperties;
            state.AppendEntriesManager = AppendEntriesManager;
            state.HeartbeatTimer = HeartbeatTimer;
        }

        private void FillCandidateDependencies(IStateDevelopment newState)
        {
            newState.StateChanger = this;

            FillCommonDependencies(newState as IStateDependencies);

            var state = newState as ICandidateDependencies;

            state.ElectionManager = ElectionManager;
            state.RemoteManager = RemoteManager;
        }

        private void FillFollowerDependencies(IStateDevelopment newState)
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
