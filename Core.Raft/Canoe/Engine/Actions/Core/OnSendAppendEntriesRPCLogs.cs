using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Canoe.Engine.States.LeaderState;
using EventGuidance.Dependency;
using EventGuidance.Structure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <summary>
    /// The leader can decide where to place new entries in the log without consulting other servers, and data flows in a simple fashion
    /// from the leader to other servers
    /// <see cref="Section 5"/>
    /// 
    /// 
    /// AppendEntriesRPC with LogEntries.Count > 0 to be sent to one single external node; only invoked from the leader.
    /// </summary>
    internal class OnSendAppendEntriesRPCLogs : EventAction<OnSendAppendEntriesRPCLogsContext, IAppendEntriesRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnSendAppendEntriesRPCLogs);
        private const string Sending = nameof(Sending);
        private const string CallObject = nameof(CallObject);
        private const string Received = nameof(Received);
        private const string ResponseObject = nameof(ResponseObject);
        private const string SendingOnException = nameof(SendingOnException);
        private const string SessionId = nameof(SessionId);
        private const string SendingOnFailure = nameof(SendingOnFailure);
        #endregion

        /// <summary>
        /// Cluster Configuration is taken dynamically, since values/uris may change
        /// </summary>
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.AppendEntriesTimeoutOnSend_InMilliseconds);

        public override string UniqueName => ActionName;

        public OnSendAppendEntriesRPCLogs(INodeConfiguration input, AppendEntriesSession session, IChangingState state, OnSendAppendEntriesRPCContextDependencies actionDependencies, IActivityLogger activityLogger = null) 
            : base(
                new OnSendAppendEntriesRPCLogsContext(state, actionDependencies)
                { 
                    NodeConfiguration = input 
                }, activityLogger)
        {
            Input.InvocationTime = DateTimeOffset.Now;
            Input.Session = session;
        }

        protected override async Task<bool> ShouldProceed()
        {
            if (!Input.IsContextValid)
            {
                return false;
            }

            Input.NextIndex = (Input.State as Leader).LeaderProperties.GetNextIndex(Input.NodeConfiguration.UniqueNodeId);
            Input.LastLogIndex = await Input.PersistentState.LogEntries.GetLastIndex();



            if (Input.NextIndex <= Input.LastLogIndex && (Input.State as Leader).AppendEntriesMonitor.CanContinue(Input)) 
            {
                return true;
            }

            return false;
        }

        protected override async Task<IAppendEntriesRPCResponse> Action(CancellationToken cancellationToken)
        {
            var currentTerm = await Input.PersistentState.GetCurrentTerm(); 
            var logEntriesToSend = await Input.PersistentState.LogEntries.FetchLogEntriesBetween(Input.NextIndex, Input.LastLogIndex);
            var previousLogEntry = await Input.PersistentState.LogEntries.TryGetValueAtIndex(logEntriesToSend.First().CurrentIndex - 1);

            /// <remarks>
            /// The leader keeps track of the highest index it knows to be committed, and it includes that index in future AppendEntries RPCs 
            /// (including heartbeats) so that the other servers eventually find out. 
            /// 
            /// Once a follower learns that a log entry is committed, it applies the entry to its local state machine(in log order).
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>

            /// <remarks>
            ///  When sending an AppendEntries RPC, the leader includes the index
            /// and term of the entry in its log that immediately precedes
            /// the new entries. If the follower does not find an entry in
            /// its log with the same index and term, then it refuses the
            /// new entries. The consistency check acts as an induction
            /// step: the initial empty state of the logs satisfies the Log
            /// Matching Property, and the consistency check preserves
            /// the Log Matching Property whenever logs are extended.
            /// 
            /// As a result, whenever AppendEntries returns successfully,
            /// the leader knows that the follower’s log is identical to its
            /// own log up through the new entries.
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>
            var callObject = new AppendEntriesRPC(entries: logEntriesToSend)
            {
                Term = currentTerm,
                LeaderCommitIndex = Input.State.VolatileState.CommitIndex,
                LeaderId = Input.ClusterConfiguration.ThisNode.UniqueNodeId,
                PreviousLogIndex = previousLogEntry.CurrentIndex,
                PreviousLogTerm = previousLogEntry.Term,

            };

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Sending AppendEntriesRPC {callObject}",
                EntitySubject = ActionName,
                Event = Sending,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(CallObject, callObject))
            .With(ActivityParam.New(SessionId, Input.Session.UniqueSessionId))
            .WithCallerInfo());

            var result = await Input.RemoteManager.Send
            (
                callObject: callObject,
                configuration: Input.NodeConfiguration,
                cancellationToken: cancellationToken
            );

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Received AppendEntriesRPCResponse {result}",
                EntitySubject = ActionName,
                Event = Received,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(ResponseObject, result))
            .With(ActivityParam.New(SessionId, Input.Session.UniqueSessionId))
            .WithCallerInfo());

            if (!result.HasResponse)
            {
                var action = new OnSendAppendEntriesRPCLogs(Input.NodeConfiguration, Input.Session, Input.State, Input.Dependencies, ActivityLogger);
                action.SupportCancellation();
                action.CancellationManager.Bind(cancellationToken);

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Queuing to SendAppendEntriesRPCLogs on exception response {result.Exception}",
                    EntitySubject = ActionName,
                    Event = SendingOnException,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ResponseObject, result))
                .With(ActivityParam.New(SessionId, Input.Session.UniqueSessionId))
                .WithCallerInfo());

                Input.Responsibilities.QueueEventAction(action: action, executeSeparately: false);
            }
            else if (result.Response.Success)
            {
                (Input.State as Leader).LeaderProperties.UpdateMatchIndex(Input.NodeConfiguration.UniqueNodeId, maxIndexReplicated: logEntriesToSend.Last().CurrentIndex);
                (Input.State as Leader).LeaderProperties.UpdateNextIndex(Input.NodeConfiguration.UniqueNodeId, maxIndexReplicated: logEntriesToSend.Last().CurrentIndex);
                (Input.State as Leader).AppendEntriesMonitor.UpdateFor(Input.Session, Input.NodeConfiguration.UniqueNodeId);

                await (Input.State as Leader).CheckIfCommitIndexNeedsUpdatation(Input.Session, Input.NodeConfiguration.UniqueNodeId);
                //await Input.State.ParallelSessionManager.RegisterSuccessResponseIfValid(Input.SessionGuid, Input.NodeConfiguration.UniqueNodeId);
            }
            else 
            {
                (Input.State as Leader).AppendEntriesMonitor.UpdateFor(Input.Session, Input.NodeConfiguration.UniqueNodeId, success: false);

                /// <remarks>
                /// 
                /// To bring a follower’s log into consistency with its own,
                /// the leader must find the latest log entry where the two
                /// logs agree, delete any entries in the follower’s log after
                /// that point, and send the follower all of the leader’s entries
                /// after that point.
                /// 
                /// All of these actions happen in response
                /// to the consistency check performed by AppendEntries
                /// RPCs.
                /// 
                /// The leader maintains a nextIndex for each follower,
                /// which is the index of the next log entry the leader will
                /// send to that follower.
                /// 
                /// When a leader first comes to power, it initializes all nextIndex values to the index just after the
                /// last one in its log. 
                /// 
                /// If a follower’s log is inconsistent with the leader’s, the AppendEntries consistency check will fail in the next AppendEntries RPC. 
                /// After a rejection, the leader decrements nextIndex and retries
                /// the AppendEntries RPC. Eventually nextIndex will reach
                /// a point where the leader and follower logs match. When
                /// this happens, AppendEntries will succeed, which removes
                /// any conflicting entries in the follower’s log and appends
                /// entries from the leader’s log. Once AppendEntries
                /// succeeds, the follower’s log is consistent with the leader’s,
                /// and it will remain that way for the rest of the term
                /// 
                /// <see cref="Section 5.3 Log Replication"/>
                /// 
                /// For retry we have to update the NextIndex to a lower value so as to send more logs to the follower who responded false.
                /// </remarks>
                await (Input.State as Leader).LeaderProperties.DecrementNextIndex(Input.NodeConfiguration.UniqueNodeId, result.Response.ConflictingEntryTermOnFailure, result.Response.FirstIndexOfConflictingEntryTermOnFailure);

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Queuing to SendAppendEntriesRPCLogs on failure response as this",
                    EntitySubject = ActionName,
                    Event = SendingOnFailure,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ResponseObject, result))
                .With(ActivityParam.New(SessionId, Input.Session.UniqueSessionId))
                .WithCallerInfo());

                /// <remarks>
                /// Queueing Event Action during State change should automatically cancel since Internal Token would have already canceled on State Change
                /// </remarks>
                Input.Responsibilities.QueueEventAction(this, executeSeparately: true);
            }

            return result.Response;
        }

        protected override Task OnTimeOut()
        {
            lock (Input)
            {
                Input.CurrentRetryCounter++;
            }
            //Interlocked.Increment(ref Input.CurrentRetryCounter);

            //TODO: TimeCalculator for tracking and calculating times


            /// <remarks>
            /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will
            /// fail.Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
            /// successfully.If a server crashes after completing an RPC but before responding, then it will receive the same RPC
            /// again after it restarts.Raft RPCs are idempotent, so this causes no harm.For example, if a follower receives an
            /// AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request
            /// <seealso cref="Section 5.5 Follower and candidate crashes"/>
            /// </remarks>
            Input.Responsibilities.QueueEventAction(this, executeSeparately: true);

            return Task.CompletedTask;
        }
    }
}
