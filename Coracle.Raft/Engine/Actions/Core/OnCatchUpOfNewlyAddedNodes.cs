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
    internal sealed class OnCatchUpOfNewlyAddedNodes : BaseAction<OnCatchUpOfNewlyAddedNodesContext, EmptyOperationResult>
    {
        #region Constants
        public const string ActionName = nameof(OnCatchUpOfNewlyAddedNodes);
        public const string NodesCaughtUp = nameof(NodesCaughtUp);
        public const string NodesNotCaughtUpYet = nameof(NodesNotCaughtUpYet);
        public const string InvalidContext = nameof(InvalidContext);
        public const string nodesToCheck = nameof(nodesToCheck);
        #endregion 

        public OnCatchUpOfNewlyAddedNodes(long logEntryIndex, string[] nodesToCheckAgainst, OnCatchUpOfNewlyAddedNodesContextDependencies input, IActivityLogger activityLogger = null) : base(new OnCatchUpOfNewlyAddedNodesContext(logEntryIndex, nodesToCheckAgainst, input)
        {
        }, activityLogger)
        {
        }

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.CatchUpOfNewNodesTimeout_InMilliseconds);
        public TimeSpan WaitInterval => TimeSpan.FromMilliseconds(Input.EngineConfiguration.CatchUpOfNewNodesWaitInterval_InMilliseconds);
        public override string UniqueName => ActionName;

        protected override async Task<EmptyOperationResult> Action(CancellationToken cancellationToken)
        {
            while (Input.CurrentStateAccessor.Get() is Leader leaderState && Input.IsContextValid)
            {
                bool caughtUp = true;

                foreach (var externalServerId in Input.NodesToCheck)
                {
                    caughtUp = caughtUp &
                        leaderState.LeaderProperties.TryGetMatchIndex(externalServerId, out var matchIndex) && matchIndex >= Input.LogEntryIndex;
                }

                if (caughtUp)
                {
                    break;
                }

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = NodesNotCaughtUpYet,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodesToCheck, Input.NodesToCheck))
                .WithCallerInfo());

                await Task.Delay(WaitInterval, cancellationToken);
            }

            if (Input.CurrentStateAccessor.Get().StateValue.IsLeader() && Input.IsContextValid)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = UniqueName,
                    Event = NodesCaughtUp,
                    Level = ActivityLogLevel.Information,
                }
                .With(ActivityParam.New(nodesToCheck, Input.NodesToCheck))
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
                    EntitySubject = UniqueName,
                    Event = InvalidContext,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodesToCheck, Input.NodesToCheck))
                .WithCallerInfo());

                return new EmptyOperationResult
                {
                    IsSuccessful = false,
                    Exception = null,
                };
            }
        }

        protected override EmptyOperationResult DefaultOutput()
        {
            return new EmptyOperationResult
            {
                IsSuccessful = false,
                Exception = null,
            };
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.NodesToCheck.Length > 0 && Input.IsContextValid && Input.CurrentStateAccessor.Get().StateValue.IsLeader());
        }
    }
}
