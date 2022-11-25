using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;
using EventGuidance.Responsibilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    internal interface IStateDependencies
    {
        IActivityLogger ActivityLogger { get; set; }
        IPersistentProperties PersistentState { get; set; }
        IGlobalAwaiter GlobalAwaiter { get; set; }
        IClientRequestHandler ClientRequestHandler { get; set; }
        IElectionTimer ElectionTimer { get; set; }
        IResponsibilities Responsibilities { get; set; }
        IClusterConfiguration ClusterConfiguration { get; set; }
        IClusterConfigurationChanger ClusterConfigurationChanger { get; set; }
        IEngineConfiguration EngineConfiguration { get; set; }
    }

    internal abstract class AbstractState : IChangingState, IStateDependencies, IHandleConfigurationChange
    {
        #region Constants

        public const string AbstractStateEntity = nameof(AbstractState);
        public const string UpdatingCommitIndex = nameof(UpdatingCommitIndex);
        public const string AbandoningState = nameof(AbandoningState);
        public const string InitializingOnStateChange = nameof(InitializingOnStateChange);
        public const string CommitGreatherThanLastApplied = nameof(CommitGreatherThanLastApplied);
        public const string NewCommitIndex = nameof(NewCommitIndex);
        public const string OldCommitIndex = nameof(OldCommitIndex);
        public const string ApplyingLogEntry = nameof(ApplyingLogEntry);
        public const string LogEntry = nameof(LogEntry);
        public const string LastApplied = nameof(LastApplied);
        public const string CommitIndex = nameof(CommitIndex);
        public const string Resuming = nameof(Resuming);
        public const string Decomissioning = nameof(Decomissioning);
        public const string Pausing = nameof(Pausing);
        public const string Exception = nameof(Exception);

        #endregion

        public IActivityLogger ActivityLogger { get; set; }
        public IPersistentProperties PersistentState { get; set; }
        public IGlobalAwaiter GlobalAwaiter { get; set; }
        public IClientRequestHandler ClientRequestHandler { get; set; }
        public IElectionTimer ElectionTimer { get; set; }
        public IResponsibilities Responsibilities { get; set; }
        public IClusterConfiguration ClusterConfiguration { get; set; }
        public IClusterConfigurationChanger ClusterConfigurationChanger { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }


        public IVolatileProperties VolatileState { get; set; }
        public StateValues StateValue { get; protected set; }
        StateValues PausedStateValue { get; set; }
        public IStateChanger StateChanger { get; set; }
        public bool IsDisposed { get; private set; } = false;

        //todo: better idea would be to pass statechanger and depChart in iniitalizeOnStateChangwe for scope modifier to be protected internal
        //public IDependencyChart Dependencies { get; set; }

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
        /// <param name="newIndex">New Commit Index to be</param>
        /// <param name="applySynchronously">Flag to control whether the control flow needs to wait until all log Entries are applied to the state machine</param>
        /// <exception cref="ArgumentException"></exception>
        internal void UpdateCommitIndex(long newIndex)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Updating Commit Index from {VolatileState.CommitIndex} to {newIndex}",
                EntitySubject = AbstractStateEntity,
                Event = UpdatingCommitIndex,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(OldCommitIndex, VolatileState.CommitIndex))
            .With(ActivityParam.New(NewCommitIndex, newIndex))
            .WithCallerInfo());

            if (newIndex <= VolatileState.CommitIndex)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"New Commit Index cannot be lesser than/equal to the old one",
                    EntitySubject = AbstractStateEntity,
                    Event = ApplyingLogEntry,
                    Level = ActivityLogLevel.Error,

                }
                .With(ActivityParam.New(NewCommitIndex, newIndex))
                .With(ActivityParam.New(OldCommitIndex, VolatileState.CommitIndex))
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

                while (VolatileState.CommitIndex > VolatileState.LastApplied)
                {
                    try
                    {

                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Updating Commit Index from {VolatileState.CommitIndex} to {newIndex}",
                            EntitySubject = AbstractStateEntity,
                            Event = CommitGreatherThanLastApplied,
                            Level = ActivityLogLevel.Debug,

                        }
                        .With(ActivityParam.New(CommitIndex, VolatileState.CommitIndex))
                        .With(ActivityParam.New(LastApplied, VolatileState.LastApplied))
                        .With(ActivityParam.New(NewCommitIndex, newIndex))
                        .WithCallerInfo());

                        VolatileState.LastApplied++;

                        var entryToApply = PersistentState.LogEntries.TryGetValueAtIndex(VolatileState.LastApplied).GetAwaiter().GetResult();


                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Applying Log Entry {entryToApply}",
                            EntitySubject = AbstractStateEntity,
                            Event = ApplyingLogEntry,
                            Level = ActivityLogLevel.Debug,

                        }
                        .With(ActivityParam.New(LogEntry, entryToApply))
                        .WithCallerInfo());

                        if (entryToApply.IsEmpty || entryToApply.IsConfiguration || !entryToApply.IsExecutable)
                            continue;

                        var commandToApply = PersistentState.LogEntries.ReadFrom<ICommand>(commandLogEntry: entryToApply).GetAwaiter().GetResult();

                        //TODO: Order should be enforced
                        ClientRequestHandler.ExecuteAndApplyLogEntry(commandToApply).RunSynchronously();
                    }
                    catch (Exception ex)
                    {
                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Caught during State Machine application for Commit Index {newIndex}",
                            EntitySubject = AbstractStateEntity,
                            Event = ApplyingLogEntry,
                            Level = ActivityLogLevel.Error,

                        }
                        .With(ActivityParam.New(Exception, ex))
                        .With(ActivityParam.New(NewCommitIndex, newIndex))
                        .WithCallerInfo());
                    }
                }

                /// <remarks>
                /// Finally Commit Index is updated. 
                /// </remarks>
                VolatileState.CommitIndex = newIndex;
            }
        }

        #endregion

        #region Configuration Change

        public virtual void HandleConfigurationChange(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {

        }

        #endregion


        #region State Change
        public virtual async Task OnStateChangeBeginDisposal()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateEntity,
                Event = AbandoningState,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            StateValue = StateValues.Abandoned;
            ElectionTimer.Dispose();
            await PersistentState.ClearVotedFor();

            Responsibilities.ConfigureNew(assigneeId: EngineConfiguration.NodeId);
        }

        public virtual Task InitializeOnStateChange(IVolatileProperties volatileProperties)
        {
            VolatileState = volatileProperties;

            //TODO: State Changer's responsibility is to make sure that new state is available in CanoeNode singleton.
            //Also LeaderNodeConfiguration should be updated RecognizedLeader when ExternalAppendEntriesRPC is received, or it elects itself as a leader. 
            //All State operations should get it from GetInstance<ICanoeNodeState>

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateEntity,
                Event = InitializingOnStateChange,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            return Task.CompletedTask;
        }

        public virtual Task OnStateEstablishment()
        {
            ElectionTimer.RegisterNew(OnElectionTimeout);

            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        public void Pause()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateEntity,
                Event = Pausing,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            PausedStateValue = StateValue;
            StateValue = StateValues.Abandoned;
        }

        public void Resume()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateEntity,
                Event = Resuming,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            StateValue = PausedStateValue;
        }

        public void Decomission()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AbstractStateEntity,
                Event = Resuming,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            StateValue = StateValues.Abandoned;

            /// Configuring new responsibilities means that EventProcessor is stopped, and supplying invokableActionNames with a random "Abandoned", means that
            /// no Enqueued actions will be able to execute.
            /// StateChanger.Initialize may have to be called to get it up and running again.
            Responsibilities.ConfigureNew(invokableActionNames: new System.Collections.Generic.HashSet<string>
            {
                StateValues.Abandoned.ToString()
            }, 
            EngineConfiguration.NodeId);
        }

        #endregion
    }
}
