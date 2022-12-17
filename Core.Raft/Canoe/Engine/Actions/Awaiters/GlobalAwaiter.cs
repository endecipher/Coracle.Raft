using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Operational;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions.Awaiters
{
    internal sealed class GlobalAwaiter : IGlobalAwaiter
    {
        #region Constants

        public const string Entity = nameof(GlobalAwaiter);
        public const string AwaitingNodesToCatchUp = nameof(AwaitingNodesToCatchUp);
        public const string AwaitingEntryCommit = nameof(AwaitingEntryCommit);
        public const string AwaitingNoDeposition = nameof(AwaitingNoDeposition);
        public const string indexToCheck = nameof(indexToCheck);
        public const string nodesToCheck = nameof(nodesToCheck);

        #endregion

        public GlobalAwaiter(
            IActivityLogger activityLogger,
            IEngineConfiguration engineConfiguration,
            ICurrentStateAccessor currentStateAccessor,
            IResponsibilities responsibilities,
            IPersistentProperties persistentProperties,
            IClusterConfiguration clusterConfiguration
            )
        {
            ActivityLogger = activityLogger;
            EngineConfiguration = engineConfiguration;
            CurrentStateAccessor = currentStateAccessor;
            Responsibilities = responsibilities;
            PersistentProperties = persistentProperties;
            ClusterConfiguration = clusterConfiguration;
        }

        public IActivityLogger ActivityLogger { get; }
        public IEngineConfiguration EngineConfiguration { get; }
        public ICurrentStateAccessor CurrentStateAccessor { get; }
        public IResponsibilities Responsibilities { get; }
        public IPersistentProperties PersistentProperties { get; }
        public IClusterConfiguration ClusterConfiguration { get; }

        public void AwaitEntryCommit(long logEntryIndex, CancellationToken cancellationToken)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = AwaitingEntryCommit,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(indexToCheck, logEntryIndex))
            .WithCallerInfo());

            var action = new OnAwaitEntryCommit(logEntryIndex, new OnAwaitEntryCommitContextDependencies
            {
                CurrentStateAccessor = CurrentStateAccessor,
                PersistentState = PersistentProperties,
                EngineConfiguration = EngineConfiguration
            }, ActivityLogger);

            action.SupportCancellation();

            action.CancellationManager.Bind(cancellationToken);

            Responsibilities.QueueBlockingEventAction<EmptyOperationResult>(action, executeSeparately: false);
        }

        public void AwaitNodesToCatchUp(long logEntryIndex, string[] nodeIdsToCheck, CancellationToken cancellationToken)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = AwaitingNodesToCatchUp,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(indexToCheck, logEntryIndex))
            .With(ActivityParam.New(nodesToCheck, nodeIdsToCheck))
            .WithCallerInfo());

            var action = new OnCatchUpOfNewlyAddedNodes(logEntryIndex, nodeIdsToCheck, new OnCatchUpOfNewlyAddedNodesContextDependencies
            {
                CurrentStateAccessor = CurrentStateAccessor,
                EngineConfiguration = EngineConfiguration
            }, ActivityLogger);

            action.SupportCancellation();

            action.CancellationManager.Bind(cancellationToken);

            Responsibilities.QueueBlockingEventAction<EmptyOperationResult>(action, executeSeparately: false);
        }

        public void AwaitNoDeposition(CancellationToken cancellationToken)
        {
            // No-Deposition here indicates, the current node, if Leader, has sent a round of Heartbeat messages, and has contacted with a Majority
            // of the cluster, without being overthrown by another Leader in the midst.

            // This is used to find if Read-only command requests can be responded as per the below:
            /// <remarks>
            /// A leader must check whether it has been deposed before processing a read-only request (its information may be stale if a more recent leader has been elected).
            /// Raft handles this by having the leader exchange heartbeat messages with a majority of the cluster before responding to read - only requests.
            /// <seealso cref="Section 8 Client Interaction"/>
            /// </remarks>
            /// 

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = AwaitingNoDeposition,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            IDictionary<string, DateTimeOffset> lastPings = null;

            while (CurrentStateAccessor.Get().StateValue.IsLeader() && !cancellationToken.IsCancellationRequested)
            {
                if (lastPings == null)
                {
                    lastPings = (CurrentStateAccessor.Get() as Leader).AppendEntriesManager.FetchLastPinged();
                }

                Task.Delay(EngineConfiguration.CheckDepositionWaitInterval_InMilliseconds, cancellationToken).Wait();

                var newPings = (CurrentStateAccessor.Get() as Leader).AppendEntriesManager.FetchLastPinged();

                bool AreMajorityServersHit()
                {
                    int clusterSize = newPings.Count;
                    int serversHit = 0;

                    foreach (var newPing in newPings)
                    {
                        if (lastPings.TryGetValue(newPing.Key, out var lastPing) && newPing.Value.CompareTo(lastPing) > 0)
                        {
                            ++serversHit;
                        }
                    }

                    int self = ClusterConfiguration.IsThisNodePartOfCluster ? 1 : 0;

                    return (serversHit + self) >= Math.Floor(clusterSize + self / 2d) + 1; // Majority Hit
                }

                if (AreMajorityServersHit())
                {
                    break;
                }
                else
                {

                    bool AreConfigurationsDifferent()
                    {
                        var oldServerIds = lastPings.Keys.ToHashSet();

                        return !oldServerIds.SetEquals(newPings.Keys);
                    }

                    /// <summary>
                    /// There is an edge-case, where a configuration change occurs within this time-frame. 
                    /// If that is the case, then we must check if the lastPingedTimes are still valid to check against, since some of the servers may be removed,
                    /// and thus, the majority condition will always fail, as the newPings will not contain those serverIds
                    /// </summary>
                    /// 

                    if (AreConfigurationsDifferent())
                    {
                        lastPings = newPings;
                    }
                }
            }
        }
    }

}
