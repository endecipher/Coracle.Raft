using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Core.Raft.Canoe.Engine.Configuration
{
    internal sealed class ClusterConfiguration : IClusterConfiguration
    {
        #region Constants

        public const string Entity = nameof(ClusterConfiguration);
        public const string NewUpdate = nameof(NewUpdate);
        public const string CurrentNodeNotPartOfCluster = nameof(CurrentNodeNotPartOfCluster);
        public const string EventKey = nameof(EventKey);

        #endregion

        object _lock = new object();

        public ClusterConfiguration(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
        }

        public ConcurrentDictionary<string, INodeConfiguration> PeerMap { get; private set; }

        public INodeConfiguration ThisNode { get; private set; }

        public bool IsThisNodePartOfCluster => ThisNode != null;

        public IEnumerable<INodeConfiguration> CurrentConfiguration
        {
            get 
            {
                var nodes = new List<NodeConfiguration>();

                nodes.Add(ThisNode.Clone() as NodeConfiguration);

                foreach (var nodeConfig in PeerMap.Values)
                {
                    nodes.Add(nodeConfig.Clone() as NodeConfiguration);
                }

                return nodes;
            }
        }

        public IActivityLogger ActivityLogger { get; }

        public IEnumerable<INodeConfiguration> Peers
        {
            get
            {
                if (PeerMap != null && PeerMap.Count > 0)
                {
                    return PeerMap.Values;
                }

                return Enumerable.Empty<INodeConfiguration>();
            }
        }

        public INodeConfiguration GetPeerNodeConfiguration(string nodeId)
        {
            return PeerMap.TryGetValue(nodeId, out var node) ? node : null;
        }

        public void UpdateConfiguration(string thisNodeId, IEnumerable<INodeConfiguration> clusterConfiguration)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = NewUpdate,
                Level = ActivityLogLevel.Debug
            }
            .WithCallerInfo());

            var map = new ConcurrentDictionary<string, INodeConfiguration>();
            INodeConfiguration thisNode = null;

            foreach (var node in clusterConfiguration)
            {
                map.TryAdd(node.UniqueNodeId, node);
            }

            bool isThisNodePartOfTheCluster = map.ContainsKey(thisNodeId);

            if (isThisNodePartOfTheCluster)
            {
                map.TryRemove(thisNodeId, out thisNode);
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = CurrentNodeNotPartOfCluster,
                    Level = ActivityLogLevel.Debug
                }
                .WithCallerInfo());
            }

            lock (_lock)
            {
                PeerMap = map;
                ThisNode = thisNode;
            }
        }
    }
}
