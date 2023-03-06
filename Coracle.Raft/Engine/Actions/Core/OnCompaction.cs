#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Raft.Engine.Actions.Core
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

        public OnCompaction(OnCompactionContextDependencies input, IStateDevelopment state, IActivityLogger activityLogger = null) : base(new OnCompactionContext(input, state)
        {
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

            if (Input.LeaderNodePronouncer.IsLeaderRecognized 
                && await Input.PersistentState.IsEligibleForCompaction(commitIndex, lastApplied, WaitPeriod, 
                    Input.EngineConfiguration.SnapshotThresholdSize, Input.EngineConfiguration.SnapshotBufferSizeFromLastEntry))
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = Eligible,
                    Level = ActivityLogLevel.Debug,
                }
                .WithCallerInfo());

                ISnapshotHeader snapshot = await Input.PersistentState
                    .Compact(commitIndex, lastApplied, Input.ClusterConfiguration.CurrentConfiguration, Input.EngineConfiguration.SnapshotThresholdSize, 
                        Input.EngineConfiguration.SnapshotBufferSizeFromLastEntry);

                await Input.PersistentState.CommitAndUpdateLog(snapshot);

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = LogCompacted,
                    Level = ActivityLogLevel.Information,
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

        protected override Task OnFailure()
        {
            Input.Responsibilities.QueueAction(this, executeSeparately: false);

            return Task.CompletedTask;
        }

        protected override Task OnTimeOut()
        {
            Input.Responsibilities.QueueAction(this, executeSeparately: false);

            return Task.CompletedTask;
        }
    }
}
