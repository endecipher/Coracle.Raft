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
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <summary>
    /// Leaders send periodic heartbeats(AppendEntries RPCs that carry no log entries) to all followers in order to maintain their authority.
    /// If a follower receives no communication over a period of time called the election timeout, then it assumes there is no viable leader 
    /// and begins an election to choose a new leader
    /// <see cref="Section 5.2 Leader Election"/>
    /// 
    /// This action is invoked as a Heartbeat to send to one External Server. 
    /// </summary>
    internal class OnSendAppendEntriesRPCHeartbeat : EventAction<OnSendAppendEntriesRPCHeartbeatContext, IAppendEntriesRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnSendAppendEntriesRPCHeartbeat);
        private const string Sending = nameof(Sending);
        private const string CallObject = nameof(CallObject);
        private const string Received = nameof(Received);
        private const string ResponseObject = nameof(ResponseObject);
        private const string SendingOnException = nameof(SendingOnException);
        private const string SessionId = nameof(SessionId);
        private const string SendingOnFailure = nameof(SendingOnFailure);
        #endregion

        public override string UniqueName => ActionName;

        public OnSendAppendEntriesRPCHeartbeat(INodeConfiguration input, AppendEntriesSession session, IChangingState state, OnSendAppendEntriesRPCContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnSendAppendEntriesRPCHeartbeatContext(state, actionDependencies)
        {
            InvocationTime = DateTimeOffset.Now,
            NodeConfiguration = input,
            Session = session,
        }, activityLogger) { }

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.AppendEntriesTimeoutOnSend_InMilliseconds);

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid && (Input.State as Leader).AppendEntriesMonitor.CanContinue(Input));
        }

        protected override async Task<IAppendEntriesRPCResponse> Action(CancellationToken cancellationToken)
        {
            var currentTerm = await Input.PersistentState.GetCurrentTerm(); 
            var lastLogEntry = await Input.PersistentState.LogEntries.TryGetValueAtLastIndex();

            /// <remarks>
            /// The leader keeps track of the highest index it knows to be committed, and it includes that index in future AppendEntries RPCs 
            /// (including heartbeats) so that the other servers eventually find out. 
            /// 
            /// Once a follower learns that a log entry is committed, it applies the entry to its local state machine(in log order).
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>

            var callObject = new AppendEntriesRPC(entries: default)
            {
                Term = currentTerm,
                LeaderCommitIndex = Input.State.VolatileState.CommitIndex,
                LeaderId = Input.ClusterConfiguration.ThisNode.UniqueNodeId,
                PreviousLogIndex = lastLogEntry.CurrentIndex,
                PreviousLogTerm = lastLogEntry.Term,
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

            var result = await Input.RemoteManager.Send(
                callObject: callObject, 
                configuration: Input.NodeConfiguration,
                cancellationToken: cancellationToken
            );

            Leader leader = (Input.State as Leader);
            
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

                /// <remarks>
                /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will fail.
                /// Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
                /// successfully. If a server crashes after completing an RPC but before responding, then it will receive the same RPC
                /// again after it restarts. 
                /// 
                /// Raft RPCs are idempotent, so this causes no harm.
                /// For example, if a follower receives an AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request.
                /// <seealso cref="Section 5.5 Follower and Candidate Crashes"/>
                /// </remarks>

                Input.Responsibilities.QueueEventAction(action: action, executeSeparately: false);
            }
            else if (result.Response.Success)
            {
                leader.LeaderProperties.UpdateMatchIndex(Input.NodeConfiguration.UniqueNodeId, maxIndexReplicated: lastLogEntry.CurrentIndex);
                leader.LeaderProperties.UpdateNextIndex(Input.NodeConfiguration.UniqueNodeId, maxIndexReplicated: lastLogEntry.CurrentIndex);
                leader.AppendEntriesMonitor.UpdateFor(Input.Session, Input.NodeConfiguration.UniqueNodeId);

                await leader.CheckIfCommitIndexNeedsUpdatation(Input.Session, Input.NodeConfiguration.UniqueNodeId);
            }
            else
            {
                leader.AppendEntriesMonitor.UpdateFor(Input.Session, Input.NodeConfiguration.UniqueNodeId, success: false);

                /// <remarks>
                /// Since this is a heartbeat action, we decrement NextIndex and issue a SendAppendEntriesLogs to the follower who responded false.
                /// </remarks>
                await leader.LeaderProperties
                    .DecrementNextIndex(Input.NodeConfiguration.UniqueNodeId, result.Response.ConflictingEntryTermOnFailure, result.Response.FirstIndexOfConflictingEntryTermOnFailure);

                /// <remarks>
                /// Queueing Event Action during State change should automatically cancel since Internal Token would have already canceled on State Change
                /// </remarks>

                var action = new OnSendAppendEntriesRPCLogs(Input.NodeConfiguration, Input.Session, Input.State, Input.Dependencies, ActivityLogger);
                action.SupportCancellation();
                action.CancellationManager.Bind(cancellationToken);

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Queuing to SendAppendEntriesRPCLogs on failure response",
                    EntitySubject = ActionName,
                    Event = SendingOnFailure,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ResponseObject, result))
                .With(ActivityParam.New(SessionId, Input.Session.UniqueSessionId))
                .WithCallerInfo());

                Input.Responsibilities.QueueEventAction(action: action, executeSeparately: true);
            }

            return result.Response;
        }

        protected override Task OnTimeOut()
        {
            lock (Input)
            {
                Input.CurrentRetryCounter++;
            }
            //Interlocked.Increment(ref CurrentRetryCounter);

            //TODO: TimeCalculator for tracking and calculating times

            /// <remarks>
            /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will
            /// fail.Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
            /// successfully.If a server crashes after completing an RPC but before responding, then it will receive the same RPC
            /// again after it restarts.Raft RPCs are idempotent, so this causes no harm.For example, if a follower receives an
            /// AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request
            /// <seealso cref="Section 5.5 Follower and candidate crashes"/>
            /// </remarks>
            Input.Responsibilities.QueueEventAction(this, true);

            return Task.CompletedTask;
        }
    }
}
