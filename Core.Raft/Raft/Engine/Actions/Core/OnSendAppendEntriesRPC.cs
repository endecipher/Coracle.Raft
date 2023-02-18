using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Actions.Core
{
    /// <summary>
    /// The leader can decide where to place new entries in the log without consulting other servers, and data flows in a simple fashion
    /// from the leader to other servers
    /// <see cref="Section 5"/>
    /// 
    /// 
    /// AppendEntriesRPC with LogEntries.Count > 0 to be sent to one single external node; only invoked from the leader.
    /// </summary>
    internal sealed class OnSendAppendEntriesRPC : BaseAction<OnSendAppendEntriesRPCLogsContext, IAppendEntriesRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnSendAppendEntriesRPC);
        public const string Sending = nameof(Sending);
        public const string RevertingToFollower = nameof(RevertingToFollower);
        public const string callObject = nameof(callObject);
        public const string exception = nameof(exception);
        public const string heartbeat = nameof(heartbeat);
        public const string towardsNode = nameof(towardsNode);
        public const string Received = nameof(Received);
        public const string responseObject = nameof(responseObject);
        public const string SendingOnException = nameof(SendingOnException);
        public const string SendingOnFailure = nameof(SendingOnFailure);
        public const string RequiresSnapshotToSend = nameof(RequiresSnapshotToSend);

        #endregion

        /// <summary>
        /// Cluster Configuration is taken dynamically, since values/uris may change
        /// </summary>
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.AppendEntriesTimeoutOnSend_InMilliseconds);

        public override string UniqueName => ActionName;

        public OnSendAppendEntriesRPC(INodeConfiguration input, IChangingState state, OnSendAppendEntriesRPCContextDependencies actionDependencies, IActivityLogger activityLogger = null)
            : base(new OnSendAppendEntriesRPCLogsContext(state, actionDependencies), activityLogger)
        {
            Input.NodeConfiguration = input;
            Input.InvocationTime = DateTimeOffset.Now;
        }

        protected override Task<bool> ShouldProceed()
        {
            // To check if state has not been abandoned, or the AppendEntries Configuration has not been changed, and that the sendee is still a CurrentNode of the Peers
            return Task.FromResult(Input.IsContextValid
                && (Input.State as Leader).AppendEntriesManager.CanSendTowards(Input.NodeConfiguration.UniqueNodeId));
        }

        protected override async Task<IAppendEntriesRPCResponse> Action(CancellationToken cancellationToken)
        {
            var currentTerm = await Input.PersistentState.GetCurrentTerm();
            (Input.State as Leader).LeaderProperties.TryGetNextIndex(Input.NodeConfiguration.UniqueNodeId, out long nextIndex);
            var lastLogIndex = await Input.PersistentState.GetLastIndex();

            AppendEntriesRPC callObject;

            // Since NextIndex should be last Index + 1, any case which has nextIndex > lastLogIndex does not require any LogEntries to be sent
            bool isHeartbeat = nextIndex > lastLogIndex;

            if (isHeartbeat)
            {
                var lastLogEntry = await Input.PersistentState.TryGetValueAtIndex(lastLogIndex);

                callObject = new AppendEntriesRPC(entries: default)
                {
                    Term = currentTerm,
                    LeaderCommitIndex = Input.State.VolatileState.CommitIndex,
                    LeaderId = Input.EngineConfiguration.NodeId, // Since current node is leader, and attempting to send outbound AppendEntries RPC
                    PreviousLogIndex = lastLogEntry.CurrentIndex,
                    PreviousLogTerm = lastLogEntry.Term,
                };
            }
            else
            {
                var logEntriesToSend = await Input.PersistentState.FetchLogEntriesBetween(nextIndex, lastLogIndex);

                long previousLogIndex = await Input.PersistentState.FetchLogEntryIndexPreviousToIndex(index: nextIndex);

                bool IsNegative(long value) => value < default(long);

                var previousLogEntry = await Input.PersistentState.TryGetValueAtIndex(IsNegative(previousLogIndex) ? default(long) : previousLogIndex);

                callObject = new AppendEntriesRPC(entries: logEntriesToSend)
                {
                    Term = currentTerm,
                    LeaderCommitIndex = Input.State.VolatileState.CommitIndex,
                    LeaderId = Input.EngineConfiguration.NodeId,
                    PreviousLogIndex = previousLogEntry?.CurrentIndex ?? default,
                    PreviousLogTerm = previousLogEntry?.Term ?? default,
                };
            }


            /// <remarks>
            /// The leader keeps track of the highest index it knows to be committed, and it includes that index in future AppendEntries RPCs 
            /// (including heartbeats) so that the other servers eventually find out. 
            /// 
            /// Once a follower learns that a log entry is committed, it applies the entry to its local state machine(in log order).
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>

            /// <remarks>
            /// When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes
            /// the new entries. If the follower does not find an entry in its log with the same index and term, then it refuses the
            /// new entries. The consistency check acts as an induction step: the initial empty state of the logs satisfies the Log
            /// Matching Property, and the consistency check preserves the Log Matching Property whenever logs are extended.
            /// 
            /// As a result, whenever AppendEntries returns successfully, the leader knows that the follower’s log is identical to its
            /// own log up through the new entries.
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ActionName,
                Event = Sending,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(OnSendAppendEntriesRPC.callObject, callObject))
            .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
            .With(ActivityParam.New(heartbeat, isHeartbeat))
            .WithCallerInfo());

            var result = await Input.RemoteManager.Send
            (
                callObject: callObject,
                configuration: Input.NodeConfiguration,
                cancellationToken: cancellationToken
            );

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ActionName,
                Event = Received,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(responseObject, result))
            .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
            .With(ActivityParam.New(heartbeat, isHeartbeat))
            .WithCallerInfo());

            Leader leader = Input.State as Leader;

            if (!result.HasResponse) //If some exception ocurred
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = SendingOnException,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(responseObject, result))
                .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                .With(ActivityParam.New(exception, result.Exception))
                .With(ActivityParam.New(heartbeat, isHeartbeat))
                .WithCallerInfo());

                /// <remarks>
                /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will fail.
                /// Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
                /// successfully. If a server crashes after completing an RPC but before responding, then it will receive the same RPC
                /// again after it restarts. 
                /// 
                /// Raft RPCs are idempotent, so this causes no harm.
                /// For example, if a follower receives an AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request.
                /// <seealso cref="Section 5.5 Follower and Candidate Crashes"/>
                /// </remarks
                leader.AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);

                return result.Response;
            }

            /// <remarks>
            /// All Servers: • If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
            /// <seealso cref="Figure 2 Rules For Servers"/>
            /// </remarks>
            /// 
            if (result.Response.Term > currentTerm)
            {
                currentTerm = result.Response.Term;

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
                .With(ActivityParam.New(responseObject, result))
                .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                .With(ActivityParam.New(heartbeat, isHeartbeat))
                .WithCallerInfo());

                await Input.PersistentState.SetCurrentTerm(currentTerm);

                Input.State.StateChanger.AbandonStateAndConvertTo<Follower>(nameof(Follower));
            }

            if (result.Response.Success)
            {
                leader.LeaderProperties.UpdateIndices(Input.NodeConfiguration.UniqueNodeId, maxIndexReplicated: lastLogIndex);
                leader.AppendEntriesManager.UpdateFor(Input.NodeConfiguration.UniqueNodeId);

                await leader.CheckIfCommitIndexNeedsUpdatation(Input.NodeConfiguration.UniqueNodeId);
            }
            else
            {
                //External Node denied AppendEntries RPC due to termMismatches or Conflicts

                /// <remarks>
                /// To bring a follower’s log into consistency with its own, the leader must find the latest log entry where the two
                /// logs agree, delete any entries in the follower’s log after that point, and send the follower all of the leader’s entries
                /// after that point. All of these actions happen in response to the consistency check performed by AppendEntries RPCs.
                /// 
                /// The leader maintains a nextIndex for each follower, which is the index of the next log entry the leader will send to that follower.
                /// When a leader first comes to power, it initializes all nextIndex values to the index just after the last one in its log. 
                /// 
                /// If a follower’s log is inconsistent with the leader’s, the AppendEntries consistency check will fail in the next AppendEntries RPC. 
                /// After a rejection, the leader DECREMENTS nextIndex and retries the AppendEntries RPC. 
                /// 
                /// Eventually nextIndex will reach a point where the leader and follower logs match. 
                /// When this happens, AppendEntries will succeed, which removes any conflicting entries in the follower’s log and appends
                /// entries from the leader’s log. 
                /// 
                /// Once AppendEntries succeeds, the follower’s log is consistent with the leader’s, and it will remain that way for the rest of the term
                /// <see cref="Section 5.3 Log Replication"/>
                /// </remarks>

                // For retry we have to update the NextIndex to a lower value so as to send more logs to the follower who responded false.

                if (result.Response.ConflictingEntryTermOnFailure.HasValue)
                {
                    await leader.LeaderProperties
                        .DecrementNextIndex(Input.NodeConfiguration.UniqueNodeId,
                            result.Response.ConflictingEntryTermOnFailure.Value, result.Response.FirstIndexOfConflictingEntryTermOnFailure.Value);
                }
                else
                {
                    await leader.LeaderProperties.DecrementNextIndex(Input.NodeConfiguration.UniqueNodeId);
                }


                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = SendingOnFailure,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(responseObject, result))
                .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                .With(ActivityParam.New(heartbeat, isHeartbeat))
                .WithCallerInfo());

                leader.AppendEntriesManager.UpdateFor(Input.NodeConfiguration.UniqueNodeId);

                /// <remarks>
                /// Queueing Event Action during State change should automatically cancel since Internal Token would have already canceled on State Change
                /// </remarks>
                leader.AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);
            }

            return result.Response;
        }

        /// <remarks>
        /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will
        /// fail.Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
        /// successfully.If a server crashes after completing an RPC but before responding, then it will receive the same RPC
        /// again after it restarts.Raft RPCs are idempotent, so this causes no harm.For example, if a follower receives an
        /// AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request
        /// <seealso cref="Section 5.5 Follower and candidate crashes"/>
        /// </remarks>
        protected override Task OnTimeOut()
        {
            (Input.State as Leader).AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);

            return Task.CompletedTask;
        }

        protected override Task OnFailure()
        {
            (Input.State as Leader).AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);

            return Task.CompletedTask;
        }

        protected override Task OnCancellation()
        {
            (Input.State as Leader).AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);

            return Task.CompletedTask;
        }
    }
}
