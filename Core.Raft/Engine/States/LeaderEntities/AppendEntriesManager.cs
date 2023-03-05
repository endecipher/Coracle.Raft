using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.ActivityLogger;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Actions;
using Coracle.Raft.Engine.Snapshots;
using Coracle.Raft.Engine.Helper;

namespace Coracle.Raft.Engine.States.LeaderEntities
{
    internal class SnapshotTracker
    {
        object _lock = new object();

        bool IsRpcSent = false;
        public ISnapshotHeader Snapshot { get; private set; }
        public bool IsInstalling => Snapshot != null;

        public bool CanSend()
        {
            lock (_lock)
            {
                if (IsRpcSent)
                {
                    ///Since an ongoing outbound request already exists
                    return false; 
                }

                /// Marking for current request
                IsRpcSent = true; 
                return true;
            }
        }

        public void OngoingRPCCompleted()
        {
            lock (_lock)
            {
                IsRpcSent = false;
            }
        }

        public bool CanStartInstallation(ISnapshotHeader snapshotHeader)
        {
            lock (_lock)
            {
                if (!IsInstalling)
                {
                    Snapshot = snapshotHeader;
                    IsRpcSent = false;
                    return true;
                }
            }

            return false;
        }

        public ISnapshotHeader StopInstallation()
        {
            ISnapshotHeader processedSnapshot = null;

            lock (_lock)
            {
                processedSnapshot = Snapshot;
                Snapshot = null;
                IsRpcSent = false;
            }

            return processedSnapshot;
        }

        public void CancelInstallation(ISnapshotHeader snapshotHeader)
        {
            string snapshotId = snapshotHeader.SnapshotId;
            long lastIncludedIndex = snapshotHeader.LastIncludedIndex;
            long lastIncludedTerm = snapshotHeader.LastIncludedTerm;

            lock (_lock)
            {
                if (IsInstalling
                    && Snapshot.SnapshotId.Equals(snapshotId) 
                    && Snapshot.LastIncludedIndex.Equals(lastIncludedIndex) 
                    && Snapshot.LastIncludedTerm.Equals(lastIncludedTerm))
                {
                    Snapshot = null;
                    IsRpcSent = false;
                }
            }
        }
    }

    internal class AppendEntriesTracker
    {
        object _lock = new object();

        bool IsRpcSent = false;

        public bool CanSend()
        {
            lock (_lock)
            {
                if (IsRpcSent)
                {
                    ///Since an ongoing outbound request already exists
                    return false; 
                }

                /// Marking for current request
                IsRpcSent = true; 
                return true;
            }
        }

        public void OngoingRPCCompleted()
        {
            lock (_lock)
            {
                IsRpcSent = false;
            }
        }
    }

    /// <remarks>
    /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will
    /// fail.Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
    /// successfully.If a server crashes after completing an RPC but before responding, then it will receive the same RPC
    /// again after it restarts.Raft RPCs are idempotent, so this causes no harm.For example, if a follower receives an
    /// AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request
    /// <seealso cref="Section 5.5 Follower and candidate crashes"/>
    /// </remarks>
    /// 

    internal class AppendEntriesManager : IAppendEntriesManager, IMembershipUpdate
    {
        #region Constants

        public const string Entity = nameof(AppendEntriesManager);
        public const string Initiate = nameof(Initiate);
        public const string InitiateSnapshot = nameof(InitiateSnapshot);
        public const string OngoingSnapshotExists = nameof(OngoingSnapshotExists);
        public const string Initializing = nameof(Initializing);
        public const string NewConfigurationManagement = nameof(NewConfigurationManagement);
        public const string nodesToAdd = nameof(nodesToAdd);
        public const string nodesToRemove = nameof(nodesToRemove);
        public const string snapshotDetails = nameof(snapshotDetails);

        #endregion

        IActivityLogger ActivityLogger { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IPersistentStateHandler PersistentState { get; }
        IOutboundRequestHandler RemoteManager { get; }
        IResponsibilities Responsibilities { get; }
        ISystemClock SystemClock { get; }

        public AppendEntriesManager(
            IActivityLogger activityLogger,
            IClusterConfiguration clusterConfiguration,
            IEngineConfiguration engineConfiguration,
            IGlobalAwaiter globalAwaiter,
            ICurrentStateAccessor currentStateAccessor,
            IPersistentStateHandler persistentProperties,
            IOutboundRequestHandler remoteManager,
            IResponsibilities responsibilities,
            ISystemClock systemClock)
        {
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            EngineConfiguration = engineConfiguration;
            GlobalAwaiter = globalAwaiter;
            CurrentStateAccessor = currentStateAccessor;
            PersistentState = persistentProperties;
            RemoteManager = remoteManager;
            Responsibilities = responsibilities;
            SystemClock = systemClock;
        }

        private object _lock = new object();

        /// <summary>
        /// Last Successful Timings stored for each Server for informational purposes
        /// </summary>
        ConcurrentDictionary<string, NodeDetails> CurrentPeers { get; set; }

        internal class NodeDetails
        {
            public INodeConfiguration NodeConfiguration { get; init; }
            public DateTimeOffset LastPinged { get; set; }

            public SnapshotTracker SnapshotTracker { get; } = new SnapshotTracker();
            public AppendEntriesTracker AppendEntriesTracker { get; } = new AppendEntriesTracker();
        }

        public void Initialize()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = Initializing,
                Level = ActivityLogLevel.Verbose,
            }
            .WithCallerInfo());

            CurrentPeers = new ConcurrentDictionary<string, NodeDetails>();

            /// Initializing to the earliest Time, since Ping not sent yet
            foreach (var config in ClusterConfiguration.Peers)
            {
                CurrentPeers.TryAdd(config.UniqueNodeId, new NodeDetails
                {
                    NodeConfiguration = config,
                    LastPinged = SystemClock.Epoch()
                });
            }
        }

        public void UpdateMembership(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {
            var newClusterMemberIds = newPeerNodeConfigurations.ToDictionary(x => x.UniqueNodeId, y => y);

            var serverIdsWhichHaveBeenRemoved = new HashSet<string>();

            foreach (var currentNodeId in CurrentPeers.Keys)
            {
                if (newClusterMemberIds.ContainsKey(currentNodeId))
                {
                    newClusterMemberIds.Remove(currentNodeId);
                }
                else
                {
                    serverIdsWhichHaveBeenRemoved.Add(currentNodeId);
                }
            }

            /// If any remaining, which have not been removed yet
            var serverIdsWhichHaveBeenAdded = newClusterMemberIds;

            /// Since we want the Cluster Configuration to change immediately, any AppendEntries actions which might be performing now
            /// will have to check CurrentNodeIds whether they can perform or not

            foreach (var node in serverIdsWhichHaveBeenAdded)
            {
                CurrentPeers.TryAdd(node.Key, new NodeDetails
                {
                    NodeConfiguration = node.Value,
                    LastPinged = SystemClock.Epoch()
                });
            }

            foreach (var nodeId in serverIdsWhichHaveBeenRemoved)
            {
                CurrentPeers.TryRemove(nodeId, out var removed);
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = NewConfigurationManagement,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(nodesToRemove, serverIdsWhichHaveBeenRemoved))
            .With(ActivityParam.New(nodesToAdd, serverIdsWhichHaveBeenAdded))
            .WithCallerInfo());
        }

        #region Append Entries

        public async void InitiateAppendEntries()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = Initiate,
                Level = ActivityLogLevel.Verbose,
            }
            .WithCallerInfo());

            var state = CurrentStateAccessor.Get();

            var nodesForInstallation = (await (state as Leader).LeaderProperties.RequiresSnapshot());

            Parallel.ForEach(source: CurrentPeers.Values, body: (nodeDetails) =>
            {
                if (nodesForInstallation != null && nodesForInstallation.TryGetValue(nodeDetails.NodeConfiguration.UniqueNodeId, out var snapshot))
                {
                    InitiateSnapshotInstallation(nodeDetails.NodeConfiguration.UniqueNodeId, snapshot).GetAwaiter().GetResult();
                }
                else
                {
                    SendAppendEntriesToNode(nodeDetails, state);
                }
            },
            parallelOptions: new ParallelOptions
            {
                CancellationToken = Responsibilities.GlobalCancellationToken,
            });
        }

        private void SendAppendEntriesToNode(NodeDetails nodeDetails, IStateDevelopment state)
        {
            var nodeId = nodeDetails.NodeConfiguration.UniqueNodeId;

            if (!nodeDetails.AppendEntriesTracker.CanSend() || nodeDetails.SnapshotTracker.IsInstalling)
                return;

            var action = new OnSendAppendEntriesRPC(input: nodeDetails.NodeConfiguration, state, new OnSendAppendEntriesRPCContextDependencies
            {
                EngineConfiguration = EngineConfiguration,
                PersistentState = PersistentState,
                RemoteManager = RemoteManager,
            }
            , ActivityLogger);

            action.SupportCancellation();

            Responsibilities.QueueAction(action, executeSeparately: false);
        }


        /// Called when an issue occurs
        public void IssueRetry(string uniqueNodeId)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
            {
                MarkNextRequestToBeSent(nodeDetails);

                new Task(() =>
                {
                    SendAppendEntriesToNode(nodeDetails, CurrentStateAccessor.Get());

                }).Start();
            }
        }

        #endregion

        #region Snapshot

        public Task CancelInstallation(string uniqueNodeId, ISnapshotHeader snapshotHeader)
        {
            if (!CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
                return Task.CompletedTask; 

            nodeDetails.SnapshotTracker.CancelInstallation(snapshotHeader);

            return Task.CompletedTask;
        }

        public async Task InitiateSnapshotInstallation(string uniqueNodeId, ISnapshotHeader snapshot)
        {
            if (!CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
                return;

            if (nodeDetails.SnapshotTracker.CanStartInstallation(snapshot))
            {
                await PersistentState.MarkInstallation(snapshot);

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = InitiateSnapshot,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(snapshotDetails, snapshot))
                .WithCallerInfo());

                new Task(() =>
                {
                    SendSnapshotChunkToNode(nodeDetails, CurrentStateAccessor.Get(), offsetToSend: default);

                }).Start();
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = OngoingSnapshotExists,
                    Level = ActivityLogLevel.Error,
                }
                .With(ActivityParam.New(snapshotDetails, nodeDetails.SnapshotTracker.Snapshot))
                .WithCallerInfo());
            }
        }

        private void SendSnapshotChunkToNode(NodeDetails nodeDetails, IStateDevelopment state, int offsetToSend)
        {
            SnapshotTracker tracker = nodeDetails.SnapshotTracker;

            if (tracker.IsInstalling && tracker.CanSend())
            {
                PersistentState.MarkInstallation(tracker.Snapshot).GetAwaiter().GetResult();

                var action = new OnSendInstallSnapshotChunkRPC(
                    input: nodeDetails.NodeConfiguration,
                    state: state,
                    snapshotHeader: tracker.Snapshot,
                    chunkOffset: offsetToSend,
                    new OnSendInstallSnapshotChunkRPCContextDependencies
                    {
                        EngineConfiguration = EngineConfiguration,
                        PersistentState = PersistentState,
                        RemoteManager = RemoteManager,
                    }
                    , ActivityLogger);

                action.SupportCancellation();

                Responsibilities.QueueAction(action, executeSeparately: false);
            }
        }

        public void IssueRetry(string uniqueNodeId, int failedOffset)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
            {
                MarkNextRequestToBeSent(nodeDetails);

                new Task(() =>
                {
                    SendSnapshotChunkToNode(nodeDetails, CurrentStateAccessor.Get(), offsetToSend: failedOffset);

                }).Start();
            }
        }

        public Task SendNextSnapshotChunk(string uniqueNodeId, int successfulOffset, bool resendAll = false)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
            {
                MarkNextRequestToBeSent(nodeDetails);

                new Task(() =>
                {
                    SendSnapshotChunkToNode(nodeDetails, CurrentStateAccessor.Get(), resendAll ? 0 : successfulOffset + 1);

                }).Start();
            }

            return Task.CompletedTask;
        }

        public async Task CompleteInstallation(string uniqueNodeId)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
            {
                await PersistentState.MarkCompletion(nodeDetails.SnapshotTracker.StopInstallation());
                InitiateAppendEntries();
            }
        }

        #endregion

        public Dictionary<string, DateTimeOffset> FetchLastPinged()
        {
            return CurrentPeers.ToDictionary(x => x.Key, y => y.Value.LastPinged);
        }

        #region Common RPC Actions
        public bool CanSendTowards(string uniqueNodeId)
        {
            return CurrentPeers.ContainsKey(uniqueNodeId);
        }

        /// Called when Node has been communicated with successfully
        public void UpdateFor(string uniqueNodeId)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
            {
                MarkNextRequestToBeSent(nodeDetails);

                nodeDetails.LastPinged = SystemClock.Now();
            }
        }

        private void MarkNextRequestToBeSent(NodeDetails nodeDetails)
        {
            nodeDetails.AppendEntriesTracker.OngoingRPCCompleted();

            nodeDetails.SnapshotTracker.OngoingRPCCompleted();
        }

        #endregion
    }
}
