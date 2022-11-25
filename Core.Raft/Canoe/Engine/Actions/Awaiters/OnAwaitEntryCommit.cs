using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Operational;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Structure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions.Awaiters
{
    internal sealed class OnAwaitEntryCommit : EventAction<OnAwaitEntryCommitContext, EmptyOperationResult>
    {
        #region Constants
        public string Entity = nameof(OnAwaitEntryCommit);
        public string EntryNotCommittedYet = nameof(EntryNotCommittedYet);
        public string EntryCommitted = nameof(EntryCommitted);
        public string InvalidContext = nameof(InvalidContext);
        public string currentCommitIndex = nameof(currentCommitIndex);
        public string indexToCheck = nameof(indexToCheck);
        #endregion 

        public OnAwaitEntryCommit(long logEntryIndex, OnAwaitEntryCommitContextDependencies actionDependencies, IActivityLogger activityLogger = null) 
            : base(new OnAwaitEntryCommitContext(logEntryIndex, actionDependencies), activityLogger)
        {
            Input.InvocationTime = DateTimeOffset.UtcNow;
        }

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.EntryCommitWaitTimeout_InMilliseconds);
        public TimeSpan WaitInterval => TimeSpan.FromMilliseconds(Input.EngineConfiguration.EntryCommitWaitInterval_InMilliseconds);
        public override string UniqueName => $"{Entity}-{Input.LogEntryIndex}";


        protected override EmptyOperationResult DefaultOutput()
        {
            return new EmptyOperationResult
            {
                IsOperationSuccessful = false,
                Exception = null,
            };
        }

        protected override async Task<EmptyOperationResult> Action(CancellationToken cancellationToken)
        {
            long commitIndex = default;

            while (Input.CurrentStateAccessor.Get().StateValue.IsLeader() && Input.IsContextValid)
            {
                commitIndex = Input.CurrentStateAccessor.Get().VolatileState.CommitIndex;

                if (commitIndex >= Input.LogEntryIndex)
                {
                    break;
                }

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = EntryNotCommittedYet,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(currentCommitIndex, commitIndex))
                .With(ActivityParam.New(indexToCheck, Input.LogEntryIndex))
                .WithCallerInfo());

                await Task.Delay(WaitInterval, cancellationToken);
            }

            if (Input.CurrentStateAccessor.Get().StateValue.IsLeader() && Input.IsContextValid) 
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = EntryCommitted,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(currentCommitIndex, commitIndex))
                .With(ActivityParam.New(indexToCheck, Input.LogEntryIndex))
                .WithCallerInfo());

                return new EmptyOperationResult
                {
                    IsOperationSuccessful = true,
                    Exception = null,
                };
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"State has changed",
                    EntitySubject = UniqueName,
                    Event = InvalidContext,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(currentCommitIndex, commitIndex))
                .With(ActivityParam.New(indexToCheck, Input.LogEntryIndex))
                .WithCallerInfo());

                return new EmptyOperationResult
                {
                    IsOperationSuccessful = false,
                    Exception = null,
                };
            }
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid && Input.CurrentStateAccessor.Get().StateValue.IsLeader());
        }
    }
}
