using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Logs;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Structure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <remarks>
    ///  Raft maintains the following properties, which together constitute the Log Matching Property - 
    ///  • If two entries in different logs have the same index and term, then they store the same command.
    ///  • If two entries in different logs have the same index and term, then the logs are identical in all preceding entries.
    /// <seealso cref="Section 5.3 Log Replication"/>
    /// </remarks>
    /// 
    /// 
    /// <summary>
    /// If we receive this, then surely there exists an External Leader Server. 
    /// </summary>
    internal sealed class OnExternalAppendEntriesRPCReceive : EventAction<OnExternalRPCReceiveContext<AppendEntriesRPC>, AppendEntriesRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnExternalAppendEntriesRPCReceive);
        public const string CommandId = nameof(CommandId);
        public const string CurrentState = nameof(CurrentState);
        public const string Response = nameof(Response);
        public const string DeniedAppendEntries = nameof(DeniedAppendEntries);
        public const string RevertingToFollower = nameof(RevertingToFollower);
        public const string Acknowledged = nameof(Acknowledged);
        public const string OverwritingEntriesIfAny= nameof(OverwritingEntriesIfAny);
        public const string ReceivedAppendEntriesCall = nameof(ReceivedAppendEntriesCall);
        public const string DeniedAppendEntriesDueToConflict = nameof(DeniedAppendEntriesDueToConflict);

        #endregion

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.AppendEntriesTimeoutOnReceive_InMilliseconds);

        public OnExternalAppendEntriesRPCReceive(AppendEntriesRPC input, IChangingState state, OnExternalRPCReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnExternalRPCReceiveContext<AppendEntriesRPC>(state, actionDependencies)
        {
            Request = input,
        }, activityLogger)
        { }

        public override string UniqueName => ActionName;

        // If Should Proceed is false, due to any reason
        protected override AppendEntriesRPCResponse DefaultOutput()
        {
            return new AppendEntriesRPCResponse
            {
                Term = Input.PersistentState.GetCurrentTerm().GetAwaiter().GetResult(),
                Success = false,
                FirstIndexOfConflictingEntryTermOnFailure = null,
                ConflictingEntryTermOnFailure = null
            };
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }

        /// <remarks>
        /// -Reply false if term < currentTerm (§5.1)
        /// -Reply false if log doesn’t contain an entry at prevLogIndex whose term matches prevLogTerm (§5.3)
        /// -If an existing entry conflicts with a new one(same index but different terms), delete the existing entry and all that
        /// follow it (§5.3)
        /// -Append any new entries not already in the log. 
        /// -If leaderCommit > commitIndex, set commitIndex = min(leaderCommit, index of last new entry)
        /// <seealso cref="Figure 2 Append Entries RPC"/>
        /// </remarks>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<AppendEntriesRPCResponse> Action(CancellationToken cancellationToken)
        {


            /// <remarks>
            /// If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
            /// <seealso cref="Figure 2 Rules For Servers"/>
            /// 
            /// If a candidate or leader discovers that its term is out of date, it immediately reverts to follower state.
            /// <seealso cref="Section 5.1 Raft Basics"/>
            /// </remarks>

            /// <remarks>
            /// While waiting for votes, a candidate may receive an AppendEntries RPC from another server claiming to be leader. If the leader’s term 
            /// (included in its RPC) is at least as large as the candidate’s current term, then the candidate recognizes the leader as legitimate and 
            /// returns to follower state. If the term in the RPC is smaller than the candidate’s current term, then the candidate rejects the RPC and 
            /// continues in candidate state.
            /// 
            /// <seealso cref="Section 5.2 Leader Election"/>
            /// </remarks>


            var currentTerm = await Input.PersistentState.GetCurrentTerm();

            if (Input.State.StateValue.IsFollower())
            {
                /// <remarks>
                /// Leader Election: a new leader must be chosen when an existing leader fails
                /// <seealso cref="Section 5"/>
                /// 
                /// When we receie an External AppendEntriesRPC, we know that it can only be sent from a leader.
                /// Thus, we reset our ElectionTimer, so that Election is delayed. 
                /// </remarks>
                /// 
                (Input.State as Follower).AcknowledgeExternalRPC();
            }

            AppendEntriesRPCResponse response;

            var logEntryAtPreviousIndex = await Input.PersistentState.LogEntries.TryGetValueAtIndex(Input.Request.PreviousLogIndex);

            /// <remarks>
            /// When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes
            /// the new entries. If the follower does not find an entry in its log with the same index and term, then it refuses the new entries.
            /// 
            /// The consistency check acts as an induction step: the initial empty state of the logs satisfies the Log Matching Property, 
            /// and the consistency check preserves the Log Matching Property whenever logs are extended.
            /// 
            /// As a result, whenever AppendEntries returns successfully, the leader knows that the follower’s log is identical to its
            /// own log up through the new entries.
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>

            if (Input.Request.Term < currentTerm)
            {
                response = new AppendEntriesRPCResponse
                {
                    Term = currentTerm,
                    Success = false,
                    FirstIndexOfConflictingEntryTermOnFailure = null, 
                    ConflictingEntryTermOnFailure = null
                };

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = DeniedAppendEntries,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ReceivedAppendEntriesCall, Input))
                .WithCallerInfo());

                return response;
            }

            if (logEntryAtPreviousIndex == null)
            {
                response = new AppendEntriesRPCResponse
                {
                    Term = currentTerm,
                    Success = false,
                    FirstIndexOfConflictingEntryTermOnFailure = Input.Request.PreviousLogIndex,
                    ConflictingEntryTermOnFailure = Input.Request.PreviousLogTerm
                };

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = DeniedAppendEntries,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ReceivedAppendEntriesCall, Input))
                .WithCallerInfo());

                return response;
            }

            /// <remarks>
            /// All Servers: • If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
            /// <seealso cref="Figure 2 Rules For Servers"/>
            /// 
            /// Here, if we are in Candidate state, and we receive an ExternalAppendEntries, that means, 
            /// in the same Term, some rival server has become Leader, and in this case, we should turn Follower.
            /// </remarks>
            /// 
            if ((Input.State.StateValue.IsCandidate() && Input.Request.Term >= currentTerm)
                        || (Input.State.StateValue.IsLeaderOrFollower() && Input.Request.Term > currentTerm))
            {
                currentTerm = Input.Request.Term;

                /// <remarks>
                /// Current terms are exchanged whenever servers communicate; if one server’s current term is smaller than the other’s, 
                /// then it updates its current term to the larger value.
                /// <seealso cref="Section 5.1 Second-to-last para"/>
                /// </remarks>
                /// 

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = RevertingToFollower,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ReceivedAppendEntriesCall, Input))
                .WithCallerInfo());

                await Input.PersistentState.SetCurrentTerm(currentTerm);

                Input.TurnToFollower = true;
            }


            if (logEntryAtPreviousIndex.Term != Input.Request.PreviousLogTerm)
            {
                /// <remarks>
                /// When rejecting an AppendEntries request, the follower can include the term of the conflicting entry and the first index it stores for that 
                /// term. With this information, the leader can decrement nextIndex to bypass all of the conflicting entries in that term; one AppendEntries RPC 
                /// will be required for each term with conflicting entries, rather than one RPC per entry
                /// <see cref="Section 5.3 Log Replication"/>
                /// </remarks>
                /// 
                response = new AppendEntriesRPCResponse
                {
                    Term = currentTerm,
                    Success = false,
                    FirstIndexOfConflictingEntryTermOnFailure = await Input.PersistentState.LogEntries.GetFirstIndexForTerm(logEntryAtPreviousIndex.Term),
                    ConflictingEntryTermOnFailure = logEntryAtPreviousIndex.Term,
                };

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = DeniedAppendEntriesDueToConflict,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ReceivedAppendEntriesCall, Input))
                .WithCallerInfo());

                return response;
            }

            // If we have reached this part of the code, that means that for the PrevLogIndex sent by the leader, we had an entry with the same term, and thus, 
            // all entries match up until the prevLogIndex.

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Overwriting Entries..",
                EntitySubject = ActionName,
                Event = OverwritingEntriesIfAny,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(ReceivedAppendEntriesCall, Input))
            .WithCallerInfo());

            /// <remarks>
            /// In Raft, the leader handles inconsistencies by forcing the followers’ logs to duplicate its own. 
            /// This means that conflicting entries in follower logs will be overwritten with entries from the leader’s log
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>

            /// <remarks>
            /// Leader Append-Only: a leader never overwrites or deletes entries in its log; it only appends new entries. §5.3
            /// <seealso cref="Figure 3"/>
            /// 
            /// If any server has applied a particular log entry to its state machine, then no other server may apply a different command for the same log index.
            /// <seealso cref="Section 5 - Safety"/>
            ///  
            /// So, one of the most important things to understand is that 
            /// 1. The leader node when they receive a client command, they write it to their log.
            /// 2. The leader then sends outbound AppendEntries RPC with log details
            /// 3. The followers write that entry in their log and confirm back
            /// 4. The leader then applies/executes that entry, and sends new AppendEntries RPC in the form of heartbeat
            /// 5. The follower nodes, then internally get their CommitIndex updated, which forces that entry's application/execution 
            /// 
            /// </remarks>
            /// 
            /// <remarks> 
            /// A leader never overwrites or deletes entries in its own log (the Leader Append - Only Property in Figure 3).
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>
            /// 

            if (Input.Request.Entries != null && Input.Request.Entries.Any())
            {
                await Input.PersistentState.LogEntries.OverwriteEntries(Input.Request.Entries);

                var configurationLogEntry = Input.Request.Entries.Where(x => x.Type.HasFlag(LogEntry.Types.Configuration)).LastOrDefault();

                if (configurationLogEntry != null)
                {
                    Input.ClusterConfigurationChanger.ApplyConfiguration(new ClusterMembershipChange
                    {
                        Configuration = await Input.PersistentState.LogEntries.ReadFrom(configurationLogEntry: configurationLogEntry),
                        ConfigurationLogEntryIndex = configurationLogEntry.CurrentIndex
                    });
                }

                if (Input.Request.LeaderCommitIndex > Input.State.VolatileState.CommitIndex)
                {
                    var newCommitIndex = Math.Min(Input.Request.LeaderCommitIndex, Input.Request.Entries.Last().CurrentIndex);

                    (Input.State as AbstractState).UpdateCommitIndex(newCommitIndex);
                }
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ActionName,
                Event = Acknowledged,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(ReceivedAppendEntriesCall, Input))
            .WithCallerInfo());

            /// <remarks>
            /// Results:
            /// term    - currentTerm,  for leader to update itself
            /// success - true,         if follower contained entry matching prevLogIndex and prevLogTerm
            /// <seealso cref="Figure 2 AppendEntriesRPC"/>
            /// </remarks>
            response = new AppendEntriesRPCResponse
            {
                Term = currentTerm,
                Success = true,
                FirstIndexOfConflictingEntryTermOnFailure = null,
                ConflictingEntryTermOnFailure = null
            };

            // Acknowledge Leader On Success
            Input.LeaderNodePronouncer.SetNewLeader(Input.Request.LeaderId);

            return response;
        }

        // This is done so that NewResponsibilities can be configured AFTER we respond to this request.
        // This is done to avoid the chances of current Task cancellation.
        protected override Task OnEventActionEnd()
        {
            if (Input.TurnToFollower)
            {
                Input.State.StateChanger.AbandonStateAndConvertTo<Follower>(nameof(Follower));
            }

            return base.OnEventActionEnd();
        }
    }
}
