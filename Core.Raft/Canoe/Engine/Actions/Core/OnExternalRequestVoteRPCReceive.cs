using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Structure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <summary>
    /// If we receive this, then surely there exists an External Candidate Fellow Server. 
    /// </summary>
    internal sealed class OnExternalRequestVoteRPCReceive : EventAction<OnExternalRPCReceiveContext<RequestVoteRPC>, RequestVoteRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnExternalRequestVoteRPCReceive);
        private const string DeniedDueToLesserTerm = nameof(DeniedDueToLesserTerm);
        private const string InputData = nameof(InputData);
        private const string DeniedSinceAlreadyVoted = nameof(DeniedSinceAlreadyVoted);
        private const string VotedFor = nameof(VotedFor);
        private const string DeniedDueToLogMismatch = nameof(DeniedDueToLogMismatch);
        private const string LastLogEntryPersisted = nameof(LastLogEntryPersisted);
        private const string DeniedByDefault = nameof(DeniedByDefault);

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
                .With(ActivityParam.New(InputData, Input))
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
                .With(ActivityParam.New(InputData, Input))
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
                .With(ActivityParam.New(InputData, Input))
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
            .With(ActivityParam.New(InputData, Input))
            .WithCallerInfo());

            return response;
        }
    }
}
