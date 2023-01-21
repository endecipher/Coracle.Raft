using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.ActivityLogger;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States.LeaderEntities
{
    /// <remarks>
    /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will
    /// fail.Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
    /// successfully.If a server crashes after completing an RPC but before responding, then it will receive the same RPC
    /// again after it restarts.Raft RPCs are idempotent, so this causes no harm.For example, if a follower receives an
    /// AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request
    /// <seealso cref="Section 5.5 Follower and candidate crashes"/>
    /// </remarks>
    /// 

    //TODO: Change this to a tighter outbound request sending machine. This should be the one which sends out multiple requests, not the states. 
    //Same for Election session
    //Alos, any outbound Event Action which fails, should send an event to this, so that retry can occur with fresh properties. Do not rely on times, like LastPinged times etc
    internal class AppendEntriesManager : IAppendEntriesManager, IHandleConfigurationChange
    {
        #region Constants

        public const string Entity = nameof(AppendEntriesManager);
        public const string Initiate = nameof(Initiate);
        public const string Initializing = nameof(Initializing);
        public const string NewConfigurationManagement = nameof(NewConfigurationManagement);
        public const string nodesToAdd = nameof(nodesToAdd);
        public const string nodesToRemove = nameof(nodesToRemove);

        #endregion

        IActivityLogger ActivityLogger { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IPersistentProperties PersistentState { get; }
        IRemoteManager RemoteManager { get; }
        IResponsibilities Responsibilities { get; }

        public AppendEntriesManager(
            IActivityLogger activityLogger,
            IClusterConfiguration clusterConfiguration,
            IEngineConfiguration engineConfiguration,
            IGlobalAwaiter globalAwaiter,
            ICurrentStateAccessor currentStateAccessor,
            IPersistentProperties persistentProperties,
            IRemoteManager remoteManager,
            IResponsibilities responsibilities)
        {
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            EngineConfiguration = engineConfiguration;
            GlobalAwaiter = globalAwaiter;
            CurrentStateAccessor = currentStateAccessor;
            PersistentState = persistentProperties;
            RemoteManager = remoteManager;
            Responsibilities = responsibilities;
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

            public bool CanSend()
            {
                lock (_lock)
                {
                    if (IsRpcSent)
                    {
                        return false; //Since an ongoing outbound request already exists
                    }
                    else
                    {
                        IsRpcSent = true; // Marking for current request
                        return true;
                    }
                }
            }

            private bool IsRpcSent = false;

            public void OngoingRPCCompleted()
            {
                lock (_lock)
                {
                    IsRpcSent = false;
                }
            }

            private object _lock = new object();
        }

        public void InitiateAppendEntries()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = Initiate,
                Level = ActivityLogLevel.Verbose,
            }
            .WithCallerInfo());

            var state = CurrentStateAccessor.Get();

            Parallel.ForEach(source: CurrentPeers.Values, body: (nodeDetails) =>
            {
                SendAppendEntriesToNode(nodeDetails, state);
            },
            parallelOptions: new ParallelOptions
            {
                CancellationToken = Responsibilities.GlobalCancellationToken,
            });
        }

        private void SendAppendEntriesToNode(NodeDetails nodeDetails, IChangingState state)
        {
            if (!nodeDetails.CanSend())
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
                    LastPinged = DateTimeOffset.UnixEpoch
                });
            }
        }

        public void HandleConfigurationChange(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
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

            // If any remaining, which have not been removed yet
            var serverIdsWhichHaveBeenAdded = newClusterMemberIds;

            // Since we want the Cluster Configuration to change immediately, any AppendEntries actions which might be performing now
            // will have to check CurrentNodeIds whether they can perform or not

            foreach (var node in serverIdsWhichHaveBeenAdded)
            {
                CurrentPeers.TryAdd(node.Key, new NodeDetails
                {
                    NodeConfiguration = node.Value,
                    LastPinged = DateTimeOffset.UnixEpoch
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

        public bool CanSendTowards(string uniqueNodeId)
        {
            return CurrentPeers.ContainsKey(uniqueNodeId);
        }

        // Called when an issue occurs
        public void IssueRetry(string uniqueNodeId)
        {
            MarkNextRequestToBeSent(uniqueNodeId);

            new Task(() =>
            {
                if (CurrentPeers.TryGetValue(uniqueNodeId, out var result))
                {
                    SendAppendEntriesToNode(result, CurrentStateAccessor.Get());
                }
            }).Start();
        }

        // Called when Node has been communicated with successfully
        public void UpdateFor(string uniqueNodeId)
        {
            MarkNextRequestToBeSent(uniqueNodeId);

            if (CurrentPeers.TryGetValue(uniqueNodeId, out var result))
            {
                result.LastPinged = DateTimeOffset.UtcNow;
            }
        }

        private void MarkNextRequestToBeSent(string uniqueNodeId)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var result))
            {
                result.OngoingRPCCompleted();
            }
        }

        public Dictionary<string, DateTimeOffset> FetchLastPinged()
        {
            return CurrentPeers.ToDictionary(x => x.Key, y => y.Value.LastPinged);
        }
    }
}
