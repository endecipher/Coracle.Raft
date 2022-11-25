using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using EventGuidance.Responsibilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States.LeaderState
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

        private const string Entity = nameof(AppendEntriesManager);
        private const string Initiate = nameof(Initiate);
        private const string Initializing = nameof(Initializing);
        private const string NewConfigurationManagement = nameof(NewConfigurationManagement);
        private const string nodesToAdd = nameof(nodesToAdd);
        private const string nodesToRemove = nameof(nodesToRemove);

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
        ConcurrentDictionary<string, (INodeConfiguration NodeConfiguration, Information Information)> CurrentPeers { get; set; }

        public struct Information
        {
            public DateTimeOffset LastPinged { get; set; }
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

            Parallel.ForEach(source: CurrentPeers.Values.Select(x=> x.NodeConfiguration), body: (nodeConfig) =>
            {
                SendAppendEntriesToNode(nodeConfig, state);
            },
            parallelOptions: new ParallelOptions
            {
                CancellationToken = Responsibilities.GlobalCancellationToken,
            });
        }

        private void SendAppendEntriesToNode(INodeConfiguration nodeConfig, IChangingState state)
        {
            var action = new OnSendAppendEntriesRPCLogs(input: nodeConfig, state, new OnSendAppendEntriesRPCContextDependencies
            {
                EngineConfiguration = EngineConfiguration,
                PersistentState = PersistentState,
                RemoteManager = RemoteManager,
            }
            , ActivityLogger);

            action.SupportCancellation();

            Responsibilities.QueueEventAction(action, executeSeparately: false);
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

            CurrentPeers = new ConcurrentDictionary<string, (INodeConfiguration NodeConfiguration, Information Information)>();

            /// Initializing to the earliest Time, since Ping not sent yet
            foreach (var config in ClusterConfiguration.Peers)
            {
                CurrentPeers.TryAdd(config.UniqueNodeId, (config, new Information { LastPinged = DateTimeOffset.UnixEpoch }));
            }
        }

        public void HandleConfigurationChange(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {
            var newClusterMemberIds = newPeerNodeConfigurations.ToDictionary(x=> x.UniqueNodeId, y => y);

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
                CurrentPeers.TryAdd(node.Key, (node.Value, new Information { LastPinged = DateTimeOffset.UnixEpoch }));
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

        public void IssueRetry(string uniqueNodeId)
        {
            UpdateFor(uniqueNodeId);

            new Task(() =>
            {
                if (CurrentPeers.TryGetValue(uniqueNodeId, out var result))
                {
                    SendAppendEntriesToNode(result.NodeConfiguration, CurrentStateAccessor.Get());
                }
            }).Start();
        }

        public void UpdateFor(string uniqueNodeId)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var result))
            {
                result.Information.LastPinged = DateTimeOffset.UtcNow;
            }
        }

        public Dictionary<string, DateTimeOffset> FetchLastPinged()
        {
            return CurrentPeers.ToDictionary(x => x.Key, y => y.Value.Information.LastPinged);
        }
    }
}
