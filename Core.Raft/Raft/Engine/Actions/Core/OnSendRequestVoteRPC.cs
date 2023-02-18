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
    /// We send this to all other servers during our election
    /// </summary>
    internal sealed class OnSendRequestVoteRPC : BaseAction<OnSendRequestVoteRPCContext, IRequestVoteRPCResponse>
    {
        #region Constants
        public const string ActionName = nameof(OnSendRequestVoteRPC);
        private const string electionTerm = nameof(electionTerm);
        private const string towardsNode = nameof(towardsNode);
        private const string Sending = nameof(Sending);
        private const string RevertingToFollower = nameof(RevertingToFollower);
        private const string callObj = nameof(callObj);
        private const string Received = nameof(Received);
        private const string responseObject = nameof(responseObject);
        private const string SendingOnException = nameof(SendingOnException);
        #endregion

        public override string UniqueName => ActionName;
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.RequestVoteTimeoutOnSend_InMilliseconds);

        public OnSendRequestVoteRPC(INodeConfiguration targetNode, long currentTerm, OnSendRequestVoteRPCContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnSendRequestVoteRPCContext(targetNode, currentTerm, actionDependencies)
        {
            InvocationTime = DateTimeOffset.UtcNow,

        }, activityLogger)
        { }

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
            var lastLogEntry = await Input.PersistentState.TryGetValueAtLastIndex();

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
            .With(ActivityParam.New(callObj, callObject))
            .With(ActivityParam.New(electionTerm, Input.ElectionTerm))
            .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
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
            .With(ActivityParam.New(responseObject, result))
            .With(ActivityParam.New(electionTerm, Input.ElectionTerm))
            .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
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
                .With(ActivityParam.New(responseObject, result))
                .With(ActivityParam.New(electionTerm, Input.ElectionTerm))
                .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                .WithCallerInfo());

                (Input.State as Candidate).ElectionManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId);

                return result.Response;
            }

            /// <remarks>
            /// All Servers: • If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
            /// <seealso cref="Figure 2 Rules For Servers"/>
            /// </remarks>
            /// 
            if (result.Response.Term > Input.ElectionTerm)
            {
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
                .With(ActivityParam.New(electionTerm, Input.ElectionTerm))
                .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                .WithCallerInfo());

                await Input.PersistentState.SetCurrentTerm(result.Response.Term);

                Input.State.StateChanger.AbandonStateAndConvertTo<Follower>(nameof(Follower));
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
