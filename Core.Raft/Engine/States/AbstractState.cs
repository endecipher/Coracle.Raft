using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Actions;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Command;

namespace Coracle.Raft.Engine.States
{
    internal interface IStateDependencies
    {
        IActivityLogger ActivityLogger { get; set; }
        IPersistentStateHandler PersistentState { get; set; }
        IGlobalAwaiter GlobalAwaiter { get; set; }
        IStateMachineHandler ClientRequestHandler { get; set; }
        IElectionTimer ElectionTimer { get; set; }
        IResponsibilities Responsibilities { get; set; }
        IClusterConfiguration ClusterConfiguration { get; set; }
        IMembershipChanger ClusterConfigurationChanger { get; set; }
        IEngineConfiguration EngineConfiguration { get; set; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    public class AbstractStateActivityConstants
    {
        #region Constants

        public const string Entity = nameof(AbstractState);
        public const string UpdatingCommitIndex = nameof(UpdatingCommitIndex);
        public const string AbandoningState = nameof(AbandoningState);
        public const string InitializingOnStateChange = nameof(InitializingOnStateChange);
        public const string CommitGreatherThanLastApplied = nameof(CommitGreatherThanLastApplied);
        public const string newCommitIndex = nameof(newCommitIndex);
        public const string oldCommitIndex = nameof(oldCommitIndex);
        public const string ApplyingLogEntry = nameof(ApplyingLogEntry);
        public const string LogEntry = nameof(LogEntry);
        public const string lastApplied = nameof(lastApplied);
        public const string commitIndex = nameof(commitIndex);
        public const string Resuming = nameof(Resuming);
        public const string Decommissioning = nameof(Decommissioning);
        public const string Stopping = nameof(Stopping);
        public const string exception = nameof(exception);
        public const string newState = nameof(newState);

        #endregion
    }

    internal abstract class AbstractState : IStateDevelopment, IStateDependencies, IMembershipUpdate
    {
        public IActivityLogger ActivityLogger { get; set; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
        public IPersistentStateHandler PersistentState { get; set; }
        public IGlobalAwaiter GlobalAwaiter { get; set; }
        public IStateMachineHandler ClientRequestHandler { get; set; }
        public IElectionTimer ElectionTimer { get; set; }
        public IResponsibilities Responsibilities { get; set; }
        public IClusterConfiguration ClusterConfiguration { get; set; }
        public IMembershipChanger ClusterConfigurationChanger { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }


        public IVolatileProperties VolatileState { get; set; }
        public StateValues StateValue { get; protected set; }
        StateValues PausedStateValue { get; set; }
        public IStateChanger StateChanger { get; set; }
        public bool IsDisposed { get; private set; } = false;

        internal AbstractState() { }

        protected abstract void OnElectionTimeout(object state);


        #region Commit Index Update

        object commitIndexLock = new object();

        /// <remarks>
        /// The leader decides when it is safe to apply a log entry to the state machines; such an entry is called committed. 
        /// Raft guarantees that committed entries are durable and will eventually be executed by all of the available state machines. 
        /// A log entry is committed once the leader that created the entry has replicated it on a majority of the servers (e.g., entry 7 in Figure 6). 
        /// This also commits all preceding entries in the leader’s log, including entries created by previous leaders
        /// <seealso cref="Section 5.3 Log Replication"/>
        /// </remarks>
        /// 
        /// <summary>
        /// If VolatileState.CommitIndex > VolatileState.LastApplied: increment VolatileState.LastApplied, apply log[VolatileState.LastApplied] to state machine.
        /// 
        /// <see cref="Figure 2 Rules For Servers | All Servers"/>
        /// </summary>
        internal void UpdateCommitIndex(long indexToAssignAsCommitIndex)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Updating Commit Index from {VolatileState.CommitIndex} to {indexToAssignAsCommitIndex}",
                EntitySubject = AbstractStateActivityConstants.Entity,
                Event = AbstractStateActivityConstants.UpdatingCommitIndex,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(AbstractStateActivityConstants.oldCommitIndex, VolatileState.CommitIndex))
            .With(ActivityParam.New(AbstractStateActivityConstants.newCommitIndex, indexToAssignAsCommitIndex))
            .WithCallerInfo());

            if (indexToAssignAsCommitIndex <= VolatileState.CommitIndex)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"New Commit Index cannot be lesser than/equal to the old one",
                    EntitySubject = AbstractStateActivityConstants.Entity,
                    Event = AbstractStateActivityConstants.ApplyingLogEntry,
                    Level = ActivityLogLevel.Error,

                }
                .With(ActivityParam.New(AbstractStateActivityConstants.newCommitIndex, indexToAssignAsCommitIndex))
                .With(ActivityParam.New(AbstractStateActivityConstants.oldCommitIndex, VolatileState.CommitIndex))
                .WithCallerInfo());

                return;
            }

            lock (commitIndexLock)
            {
                /// <remarks>
                /// Raft never commits log entries from previous terms by counting replicas. Only log entries from the leader’s current
                /// term are committed by counting replicas; once an entry from the current term has been committed in this way,
                /// then all prior entries are committed indirectly because of the Log Matching Property.
                /// 
                /// There are some situations where a leader could safely conclude that an older log entry is committed
                /// (for example, if that entry is stored on every server), but Raft takes a more conservative approach for simplicity.
                /// <seealso cref="Section 5.4.2 Committing entries from previous terms"/>
                /// </remarks>

                while (indexToAssignAsCommitIndex > VolatileState.LastApplied)
                {
                    try
                    {

                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Updating Commit Index from {VolatileState.CommitIndex} to {indexToAssignAsCommitIndex}",
                            EntitySubject = AbstractStateActivityConstants.Entity,
                            Event = AbstractStateActivityConstants.CommitGreatherThanLastApplied,
                            Level = ActivityLogLevel.Debug,

                        }
                        .With(ActivityParam.New(AbstractStateActivityConstants.commitIndex, VolatileState.CommitIndex))
                        .With(ActivityParam.New(AbstractStateActivityConstants.lastApplied, VolatileState.LastApplied))
                        .With(ActivityParam.New(AbstractStateActivityConstants.newCommitIndex, indexToAssignAsCommitIndex))
                        .WithCallerInfo());

                        VolatileState.LastApplied++;

                        var entryToApply = PersistentState.TryGetValueAtIndex(VolatileState.LastApplied).GetAwaiter().GetResult();

                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Applying Log Entry {entryToApply}",
                            EntitySubject = AbstractStateActivityConstants.Entity,
                            Event = AbstractStateActivityConstants.ApplyingLogEntry,
                            Level = ActivityLogLevel.Debug,

                        }
                        .With(ActivityParam.New(AbstractStateActivityConstants.LogEntry, entryToApply))
                        .WithCallerInfo());

                        if (entryToApply == null || !entryToApply.Type.HasFlag(Logs.LogEntry.Types.Command))
                            continue;

                        var commandToApply = PersistentState.ReadFrom<ICommand>(commandLogEntry: entryToApply).GetAwaiter().GetResult();

                        ClientRequestHandler.ExecuteAndApply(commandToApply);
                    }
                    catch (Exception ex)
                    {
                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Caught during State Machine application for Commit Index {indexToAssignAsCommitIndex}",
                            EntitySubject = AbstractStateActivityConstants.Entity,
                            Event = AbstractStateActivityConstants.ApplyingLogEntry,
                            Level = ActivityLogLevel.Error,

                        }
                        .With(ActivityParam.New(AbstractStateActivityConstants.exception, ex))
                        .With(ActivityParam.New(AbstractStateActivityConstants.newCommitIndex, indexToAssignAsCommitIndex))
                        .WithCallerInfo());
                    }
                }

                /// <remarks>
                /// Finally Commit Index is updated. 
                /// Just in case an earlier indexToAssign is supplied parallely. Better than assigning directly.
                /// </remarks>

                VolatileState.CommitIndex = Math.Max(indexToAssignAsCommitIndex, VolatileState.CommitIndex);
            }
        }

        #endregion

        #region Configuration Change

        public virtual void UpdateMembership(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {

        }

        #endregion

        #region State Change
        public virtual async Task OnStateChangeBeginDisposal()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateActivityConstants.Entity,
                Event = AbstractStateActivityConstants.AbandoningState,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            StateValue = StateValues.Stopped;
            ElectionTimer.Dispose();
            await PersistentState.ClearVotedFor();

            Responsibilities.ConfigureNew(identifier: EngineConfiguration.NodeId);
        }

        public virtual Task InitializeOnStateChange(IVolatileProperties volatileProperties)
        {
            VolatileState = volatileProperties;

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateActivityConstants.Entity,
                Event = AbstractStateActivityConstants.InitializingOnStateChange,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            return Task.CompletedTask;
        }

        public virtual Task OnStateEstablishment()
        {
            ElectionTimer.RegisterNew(OnElectionTimeout);

            var logCompaction = new OnCompaction(new Actions.Contexts.OnCompactionContextDependencies
            {
                EngineConfiguration = EngineConfiguration,
                LeaderNodePronouncer = LeaderNodePronouncer,
                PersistentState = PersistentState,
                Responsibilities = Responsibilities,
                ClusterConfiguration = ClusterConfiguration

            }, this, ActivityLogger);

            logCompaction.SupportCancellation();

            Responsibilities.QueueAction(logCompaction, executeSeparately: false);

            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        public void Stop()
        {
            if (StateValue.IsAbandoned())
                throw Exceptions.AbandonedStateCannotBeControlledException.New();

            PausedStateValue = StateValue;

            StateValue = StateValues.Stopped;

            /// Configuring new responsibilities means that EventProcessor is stopped, and supplying invokableActionNames with a string "Stopped", means that
            /// no Enqueued actions will be able to execute.
            /// 
            /// StateChanger.Initialize may have to be called to get it up and running again.
            Responsibilities.ConfigureNew(invocableActionNames: new HashSet<string>
            {
                StateValues.Stopped.ToString()
            },
            EngineConfiguration.NodeId);

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateActivityConstants.Entity,
                Event = AbstractStateActivityConstants.Stopping,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(AbstractStateActivityConstants.newState, StateValue.ToString()))
            .WithCallerInfo());
        }

        public void Resume()
        {
            if (StateValue.IsAbandoned())
                throw Exceptions.AbandonedStateCannotBeControlledException.New();

            StateValue = PausedStateValue;

            Responsibilities.ConfigureNew(null, EngineConfiguration.NodeId);

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateActivityConstants.Entity,
                Event = AbstractStateActivityConstants.Resuming,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(AbstractStateActivityConstants.newState, StateValue.ToString()))
            .WithCallerInfo());
        }

        public void Decommission()
        {
            StateValue = StateValues.Abandoned;

            /// Configuring new responsibilities means that EventProcessor is stopped, and supplying invokableActionNames with a random "Abandoned", means that
            /// no Enqueued actions will be able to execute. 
            Responsibilities.ConfigureNew(invocableActionNames: new HashSet<string>
            {
                StateValues.Abandoned.ToString()
            },
            EngineConfiguration.NodeId);

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateActivityConstants.Entity,
                Event = AbstractStateActivityConstants.Decommissioning,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(AbstractStateActivityConstants.newState, StateValue.ToString()))
            .WithCallerInfo());
        }

        #endregion
    }
}
