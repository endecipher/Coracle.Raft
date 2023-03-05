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
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Actions.Core
{
    internal sealed class OnAwaitEntryCommit : BaseAction<OnAwaitEntryCommitContext, EmptyOperationResult>
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
        }

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.EntryCommitWaitTimeout_InMilliseconds);
        public TimeSpan WaitInterval => TimeSpan.FromMilliseconds(Input.EngineConfiguration.EntryCommitWaitInterval_InMilliseconds);
        public override string UniqueName => $"{Entity}-{Input.LogEntryIndex}";


        protected override EmptyOperationResult DefaultOutput()
        {
            return new EmptyOperationResult
            {
                IsSuccessful = false,
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
                    IsSuccessful = true,
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
                    IsSuccessful = false,
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
