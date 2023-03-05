using ActivityLogger.Logging;
using Coracle.Raft.Engine.ActivityLogger;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    public class ActivityConstants
    {
        #region Constants

        public const string Entity = nameof(ClusterConfiguration);
        public const string NewUpdate = nameof(NewUpdate);
        public const string CurrentNodeNotPartOfCluster = nameof(CurrentNodeNotPartOfCluster);
        public const string allNodeIds = nameof(allNodeIds);

        #endregion
    }

    internal sealed class ClusterConfiguration : IClusterConfiguration
    {
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
                if (PeerMap == null)
                    return Enumerable.Empty<INodeConfiguration>();

                return IsThisNodePartOfCluster ? PeerMap.Values.Append(ThisNode) : PeerMap.Values;
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
                    EntitySubject = ActivityConstants.Entity,
                    Event = ActivityConstants.CurrentNodeNotPartOfCluster,
                    Level = ActivityLogLevel.Debug
                }
                .WithCallerInfo());
            }

            lock (_lock)
            {
                PeerMap = map;
                ThisNode = thisNode;
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ActivityConstants.Entity,
                Event = ActivityConstants.NewUpdate,
                Level = ActivityLogLevel.Debug
            }
            .With(ActivityParam.New(ActivityConstants.allNodeIds, CurrentConfiguration.Select(_ => _.UniqueNodeId).ToArray()))
            .WithCallerInfo());
        }
    }
}
