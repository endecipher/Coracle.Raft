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
    internal sealed class OnCatchUpOfNewlyAddedNodes : EventAction<OnCatchUpOfNewlyAddedNodesContext, EmptyOperationResult>
    {
        #region Constants
        public string Entity = nameof(OnCatchUpOfNewlyAddedNodes);
        public string NodesCaughtUp = nameof(NodesCaughtUp);
        public string NodesNotCaughtUpYet = nameof(NodesNotCaughtUpYet);
        public string InvalidContext = nameof(InvalidContext);
        public string nodesToCheck = nameof(nodesToCheck);
        #endregion 

        public OnCatchUpOfNewlyAddedNodes(long logEntryIndex, string[] nodesToCheckAgainst, OnCatchUpOfNewlyAddedNodesContextDependencies input, IActivityLogger activityLogger = null) : base(new OnCatchUpOfNewlyAddedNodesContext(logEntryIndex, nodesToCheckAgainst, input)
        {
            InvocationTime = DateTimeOffset.UtcNow
        }, activityLogger)
        {
        }

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.CatchUpOfNewNodesTimeout_InMilliseconds);
        public TimeSpan WaitInterval => TimeSpan.FromMilliseconds(Input.EngineConfiguration.CatchUpOfNewNodesWaitInterval_InMilliseconds);
        public override string UniqueName => Entity;

        protected override async Task<EmptyOperationResult> Action(CancellationToken cancellationToken)
        {
            while ((Input.CurrentStateAccessor.Get() is Leader leaderState) && Input.IsContextValid)
            {
                bool caughtUp = true;

                foreach (var externalServerId in Input.NodesToCheck)
                {
                    caughtUp = caughtUp & 
                        leaderState.LeaderProperties.MatchIndexForServers.TryGetValue(externalServerId, out var matchIndex) && matchIndex >= Input.LogEntryIndex;
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
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodesToCheck, Input.NodesToCheck))
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
                    EntitySubject = UniqueName,
                    Event = InvalidContext,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodesToCheck, Input.NodesToCheck))
                .WithCallerInfo());

                return new EmptyOperationResult
                {
                    IsOperationSuccessful = false,
                    Exception = null,
                };
            }
        }

        protected override EmptyOperationResult DefaultOutput()
        {
            return new EmptyOperationResult
            {
                IsOperationSuccessful = false,
                Exception = null,
            };
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.NodesToCheck.Length > 0 && Input.IsContextValid && Input.CurrentStateAccessor.Get().StateValue.IsLeader());
        }
    }
}
