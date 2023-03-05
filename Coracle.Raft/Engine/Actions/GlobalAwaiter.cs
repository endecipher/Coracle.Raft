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
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Raft.Engine.Actions
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
            IPersistentStateHandler persistentProperties,
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
        public IPersistentStateHandler PersistentProperties { get; }
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

            Responsibilities.QueueBlockingAction<EmptyOperationResult>(action, executeSeparately: false);
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

            Responsibilities.QueueBlockingAction<EmptyOperationResult>(action, executeSeparately: false);
        }

        public void AwaitNoDeposition(CancellationToken cancellationToken)
        {
            /// No-Deposition here indicates that the current node, if Leader, has sent a round of Heartbeat messages, and has contacted with a Majority
            /// of the cluster, without being overthrown by another Leader in the midst.

            /// <remarks>
            /// A leader must check whether it has been deposed before processing a read-only request (its information may be stale if a more recent leader has been elected).
            /// Raft handles this by having the leader exchange heartbeat messages with a majority of the cluster before responding to read - only requests.
            /// <seealso cref="Section 8 Client Interaction"/>
            /// </remarks>

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = AwaitingNoDeposition,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());

            IDictionary<string, DateTimeOffset> lastPings = null;
            IDictionary<string, DateTimeOffset> newPings = null;

            while (CurrentStateAccessor.Get().StateValue.IsLeader() && !cancellationToken.IsCancellationRequested)
            {
                if (lastPings == null || newPings == null || lastPings.Keys.Count != newPings.Keys.Count || newPings.Keys.Except(lastPings.Keys).Any())
                {
                    lastPings = (CurrentStateAccessor.Get() as Leader).AppendEntriesManager.FetchLastPinged();
                }

                Task.Delay(EngineConfiguration.CheckDepositionWaitInterval_InMilliseconds, cancellationToken).Wait();

                newPings = (CurrentStateAccessor.Get() as Leader).AppendEntriesManager.FetchLastPinged();

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

                    return Majority.HasAttained(serversHit + self, clusterSize + self); 
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

                    if (AreConfigurationsDifferent())
                    {
                        lastPings = newPings;
                    }
                }
            }
        }
    }

}
