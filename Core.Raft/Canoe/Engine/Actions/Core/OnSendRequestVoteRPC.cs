using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Dependency;
using EventGuidance.Responsibilities;
using EventGuidance.Structure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <summary>
    /// We send this to all other servers during our election
    /// </summary>
    internal class OnSendRequestVoteRPC : EventAction<OnSendRequestVoteRPCContext, IRequestVoteRPCResponse>
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
        int MaxRetryCounter => Input.EngineConfiguration.SendRequestVoteRPC_MaxRetryInfinityCounter;

        public OnSendRequestVoteRPC(INodeConfiguration input, Guid sessionGuid, IChangingState state, OnSendRequestVoteRPCContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnSendRequestVoteRPCContext(state, actionDependencies)
        {
            SessionGuid = sessionGuid,
            InvocationTime = DateTimeOffset.UtcNow,
            NodeConfiguration = input
        }, activityLogger) { }

        /// <summary>
        /// We will fail if a previous Election's Event Action is being fired
        /// </summary>
        /// <returns></returns>
        protected override Task<bool> ShouldProceed()
        {
            //TODO: Across all ShouldProceeds for any sort of Event Action, shouldn't we ensure that the State is disposed properly, 
            //Since on retry, we may be acting on stale data, IF cancellation Tokens do not do the job?

            if (Input.IsContextValid && Input.CurrentRetryCounter < MaxRetryCounter && (Input.State as Candidate).ElectionSession.SessionGuid.Equals(Input.SessionGuid))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Framing Request
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<IRequestVoteRPCResponse> Action(CancellationToken cancellationToken)
        {
            var currentTerm = await Input.PersistentState.GetCurrentTerm();
            var lastLogEntry = await Input.PersistentState.LogEntries.TryGetValueAtLastIndex();

            var callObject = new RequestVoteRPC
            {
                Term = currentTerm,
                CandidateId = Input.ClusterConfiguration.ThisNode.UniqueNodeId,
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

                Input.Responsibilities.QueueEventAction(action: this, executeSeparately: false);
            }
            else if (result.Response.VoteGranted)
            {
                (Input.State as Candidate).ElectionSession.ReceiveVoteFrom(Input.SessionGuid, Input.NodeConfiguration.UniqueNodeId);
            }
            else
            {
                (Input.State as Candidate).ElectionSession.ReceiveNoConfidenceVoteFrom(Input.SessionGuid, Input.NodeConfiguration.UniqueNodeId); 
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

            Input.Responsibilities.QueueEventAction(this, true);

            return Task.CompletedTask;
        }
    }
}
