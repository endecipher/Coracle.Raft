using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
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
    /// If we receive this, then surely there exists an External Candidate Fellow Server. 
    /// </summary>
    internal sealed class OnExternalRequestVoteRPCReceive : BaseAction<OnExternalRPCReceiveContext<RequestVoteRPC>, RequestVoteRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnExternalRequestVoteRPCReceive);
        public const string DeniedDueToLesserTerm = nameof(DeniedDueToLesserTerm);
        public const string BeingFollowerAsGreaterTermReceived = nameof(BeingFollowerAsGreaterTermReceived);
        public const string inputRequest = nameof(inputRequest);
        public const string DeniedSinceAlreadyVoted = nameof(DeniedSinceAlreadyVoted);
        public const string VotedFor = nameof(VotedFor);
        public const string DeniedDueToLogMismatch = nameof(DeniedDueToLogMismatch);
        public const string LastLogEntryPersisted = nameof(LastLogEntryPersisted);
        public const string DeniedByDefault = nameof(DeniedByDefault);

        #endregion


        public override string UniqueName => ActionName;
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.RequestVoteTimeoutOnReceive_InMilliseconds);

        public OnExternalRequestVoteRPCReceive(RequestVoteRPC input, IChangingState state, OnExternalRPCReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnExternalRPCReceiveContext<RequestVoteRPC>(state, actionDependencies)
        {
            Request = input
        }, activityLogger)
        { }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }

        protected override async Task<RequestVoteRPCResponse> Action(CancellationToken cancellationToken)
        {
            long Term = await Input.PersistentState.GetCurrentTerm();
            string votedFor = await Input.PersistentState.GetVotedFor();

            var response = new RequestVoteRPCResponse
            {
                Term = Term,
                VoteGranted = false
            };


            /// <remarks>
            /// All Servers: • If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
            /// <seealso cref="Figure 2 Rules For Servers"/>
            /// </remarks>
            /// 
            if (Input.Request.Term > Term)
            {
                Term = Input.Request.Term;

                await Input.PersistentState.SetCurrentTerm(Term);


                if (Input.State.StateValue.IsCandidate() || Input.State.StateValue.IsLeader())
                {
                    Input.TurnToFollower = true;
                }
                else if (Input.State.StateValue.IsFollower())
                {
                    (Input.State as Follower).AcknowledgeExternalRPC();
                }
                
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = BeingFollowerAsGreaterTermReceived,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .WithCallerInfo());

                // Updating Response since Term has changed
                response = new RequestVoteRPCResponse
                {
                    Term = Term,
                    VoteGranted = false
                };
            }

            /// <remarks>
            ///  Reply false if term < currentTerm 
            /// <see cref="Figure 2 RequestVoteRPC"/>
            /// </remarks>
            if (Input.Request.Term < Term)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Denied RPC due to lesser Term.",
                    EntitySubject = ActionName,
                    Event = DeniedDueToLesserTerm,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .WithCallerInfo());

                return response;
            }

            /// <remarks>
            /// Each server will vote for at most one candidate in a
            /// given term, on a first - come - first - served basis. 
            /// 
            /// <see cref="Section 5.2 Leader Election"/>
            /// 
            /// VotedFor will be updated on a successful vote.
            /// </remarks>
            if (!string.IsNullOrWhiteSpace(votedFor))
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Denied RPC since already casted vote for {votedFor}",
                    EntitySubject = ActionName,
                    Event = DeniedSinceAlreadyVoted,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .With(ActivityParam.New(VotedFor, votedFor))
                .WithCallerInfo());

                return response;
            }

            /// <remarks>
            /// Raft uses the voting process to prevent a candidate from
            /// winning an election unless its log contains all committed entries.A candidate must contact a majority of the cluster
            /// in order to be elected, which means that every committed entry must be present in at least one of those servers. If the
            /// candidate’s log is at least as up - to - date as any other log in that majority(where “up-to - date” is defined precisely
            /// below), then it will hold all the committed entries. 
            /// 
            /// The RequestVote RPC implements this restriction: the RPC includes information about the candidate’s log, and the voter denies its 
            /// vote if its own log is more up - to - date than that of the candidate. Raft determines which of two logs is more up - to - date
            /// by comparing the index and term of the last entries in the logs.If the logs have last entries with different terms, then
            /// the log with the later term is more up - to - date.If the logs end with the same term, then whichever log is longer is
            /// more up - to - date.
            /// <seealso cref="Section 5.4.1 Election Restriction"/>
            /// </remarks>

            /// <remarks>
            ///  If votedFor is null or candidateId, and candidate’s log is at least as up-to-date as receiver’s log, grant vote
            ///  
            /// <seealso cref="Figure 2 RequestVoteRPC"/>
            /// </remarks>
            var lastLogEntryPersisted = await Input.PersistentState.LogEntries.TryGetValueAtLastIndex();

            if (lastLogEntryPersisted.CurrentIndex <= Input.Request.LastLogIndex && lastLogEntryPersisted.Term <= Input.Request.LastLogTerm)
            {
                response = new RequestVoteRPCResponse
                {
                    Term = Term,
                    VoteGranted = true
                };


                /// <remarks>
                /// A candidate wins an election if it receives votes from a majority of the servers in the full cluster for the same term.
                /// Each server will vote for at most one candidate in a given term, on a first - come - first - served basis.
                /// The majority rule ensures that at most one candidate can win the election for a particular term
                /// <see cref="Section 5.2 Leader Election"/>
                /// </remarks>
                await Input.PersistentState.SetVotedFor(Input.Request.CandidateId);

                return response;
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Denied RPC due to candidate’s log not being at least as up-to-date as current persisted log",
                    EntitySubject = ActionName,
                    Event = DeniedDueToLogMismatch,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .With(ActivityParam.New(LastLogEntryPersisted, lastLogEntryPersisted))
                .WithCallerInfo());
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Could not process RequestVoteRPC. Failing request by default..",
                EntitySubject = ActionName,
                Event = DeniedByDefault,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(inputRequest, Input.Request))
            .WithCallerInfo());

            return response;
        }

        // This is done so that NewResponsibilities can be configured AFTER we respond to this request.
        // This is done to avoid the chances of current Task cancellation.
        protected override Task OnActionEnd()
        {
            if (Input.TurnToFollower)
            {
                Input.State.StateChanger.AbandonStateAndConvertTo<Follower>(nameof(Follower));
            }

            return base.OnActionEnd();
        }
    }
}
