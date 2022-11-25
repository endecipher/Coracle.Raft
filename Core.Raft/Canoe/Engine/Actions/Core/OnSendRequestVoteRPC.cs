using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Structure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <summary>
    /// We send this to all other servers during our election
    /// </summary>
    internal sealed class OnSendRequestVoteRPC : EventAction<OnSendRequestVoteRPCContext, IRequestVoteRPCResponse>
    {
        #region Constants
        public const string ActionName = nameof(OnSendRequestVoteRPC);
        private const string InputData = nameof(InputData);
        private const string Sending = nameof(Sending);
        private const string CallObject = nameof(CallObject);
        private const string Received = nameof(Received);
        private const string ResponseObject = nameof(ResponseObject);
        private const string SendingOnException = nameof(SendingOnException);
        #endregion
        
        public override string UniqueName => ActionName;
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.RequestVoteTimeoutOnSend_InMilliseconds);

        public OnSendRequestVoteRPC(INodeConfiguration targetNode, long currentTerm, OnSendRequestVoteRPCContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnSendRequestVoteRPCContext(targetNode, currentTerm, actionDependencies)
        {
            InvocationTime = DateTimeOffset.UtcNow,

        }, activityLogger) { }

        /// <summary>
        /// We will fail if a previous Election's Event Action is being fired
        /// </summary>
        /// <returns></returns>
        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid && (Input.State as Candidate).ElectionManager.CanSendTowards(Input.NodeConfiguration.UniqueNodeId, Input.ElectionTerm));
        }

        /// <summary>
        /// Framing Request
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<IRequestVoteRPCResponse> Action(CancellationToken cancellationToken)
        {
            var lastLogEntry = await Input.PersistentState.LogEntries.TryGetValueAtLastIndex();

            var callObject = new RequestVoteRPC
            {
                Term = Input.ElectionTerm,
                CandidateId = Input.EngineConfiguration.NodeId,
                LastLogIndex = lastLogEntry.CurrentIndex,
                LastLogTerm = lastLogEntry.Term
            };

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Sending RequestVoteRPC {callObject}",
                EntitySubject = ActionName,
                Event = Sending,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(CallObject, callObject))
            .With(ActivityParam.New(InputData, Input))
            .WithCallerInfo());

            var result = await Input.RemoteManager.Send
            (
                callObject: callObject, 
                configuration: Input.NodeConfiguration,
                cancellationToken: cancellationToken
            );

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Received RequestVoteRPCResponse {result}",
                EntitySubject = ActionName,
                Event = Received,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(ResponseObject, result))
            .With(ActivityParam.New(InputData, Input))
            .WithCallerInfo());

            if (!result.HasResponse)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Queuing to SendRequestVoteRPC on exception response {result.Exception}",
                    EntitySubject = ActionName,
                    Event = SendingOnException,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(ResponseObject, result))
                .With(ActivityParam.New(InputData, Input))
                .WithCallerInfo());

                (Input.State as Candidate).ElectionManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);
            }
            else
            {
                (Input.State as Candidate).ElectionManager.UpdateFor(result.Response.Term, Input.NodeConfiguration.UniqueNodeId, result.Response.VoteGranted); 
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
            (Input.State as Candidate).ElectionManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);

            return Task.CompletedTask;
        }

        protected override Task OnFailure()
        {
            (Input.State as Candidate).ElectionManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);

            return Task.CompletedTask;
        }
    }
}
