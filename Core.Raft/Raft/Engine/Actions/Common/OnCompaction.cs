using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Raft.Engine.Actions.Common
{
    internal sealed class OnCompaction : BaseAction<OnCompactionContext, object>
    {
        #region Constants
        public const string ActionName = nameof(OnCompaction);
        public const string NotEligible = nameof(NotEligible);
        public const string LogCompacted = nameof(LogCompacted);
        public const string Eligible = nameof(Eligible);
        public const string snapshot = nameof(snapshot);
        #endregion 

        public OnCompaction(OnCompactionContextDependencies input, IChangingState state, IActivityLogger activityLogger = null) : base(new OnCompactionContext(input, state)
        {
            InvocationTime = DateTimeOffset.UtcNow
        }, activityLogger)
        {
        }

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.CompactionAttemptTimeout_InMilliseconds);
        public TimeSpan WaitInterval => TimeSpan.FromMilliseconds(Input.EngineConfiguration.CompactionAttemptInterval_InMilliseconds);
        public TimeSpan WaitPeriod => TimeSpan.FromMilliseconds(Input.EngineConfiguration.CompactionWaitPeriod_InMilliseconds);
        public override string UniqueName => ActionName;
        public override ActionPriorityValues PriorityValue => ActionPriorityValues.Low;


        protected override async Task<object> Action(CancellationToken cancellationToken)
        {
            var commitIndex = Input.State.VolatileState.CommitIndex;
            var lastApplied = Input.State.VolatileState.LastApplied;

            if (Input.LeaderNodePronouncer.IsLeaderRecognized && await Input.PersistentState.IsEligibleForCompaction(commitIndex, lastApplied, WaitPeriod))
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = Eligible,
                    Level = ActivityLogLevel.Debug,
                }
                .WithCallerInfo());

                ISnapshotHeader snapshot = await Input.PersistentState.Compact(commitIndex, lastApplied);

                await Input.PersistentState.CommitAndUpdateLog(snapshot);

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = LogCompacted,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(OnCompaction.snapshot, snapshot))
                .WithCallerInfo());

                return null;
            }
            else
            {
                await Task.Delay(WaitInterval, cancellationToken);

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = NotEligible,
                    Level = ActivityLogLevel.Debug,
                }
                .WithCallerInfo());

                return null;
            }
        }

        protected override object DefaultOutput()
        {
            return null;
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }

        protected override Task OnActionEnd()
        {
            Input.Responsibilities.QueueAction(this, executeSeparately: false);

            return Task.CompletedTask;
        }
    }
}
