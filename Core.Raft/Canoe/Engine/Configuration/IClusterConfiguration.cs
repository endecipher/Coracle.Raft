using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Core.Raft.Canoe.Engine.Configuration
{

    /// <remarks>
    /// For the configuration change mechanism to be safe, there must be no point during the transition where it
    /// is possible for two leaders to be elected for the same term. Unfortunately, any approach where servers switch
    /// directly from the old configuration to the new configuration is unsafe. It isn’t possible to atomically switch all of
    /// the servers at once, so the cluster can potentially split into two independent majorities during the transition
    /// <seealso cref="Section 6 Cluster membership changes"/>
    /// </remarks>

    /// <summary>
    /// In-Memory track of Node Configurations
    /// </summary>
    public interface IClusterConfiguration
    {
        public ConcurrentDictionary<string, INodeConfiguration> Peers { get; }

        public INodeConfiguration ThisNode { get; }

        void UpdateConfiguration(string thisNodeId, IEnumerable<INodeConfiguration> allNodes);
    }

    internal sealed class ClusterConfiguration : IClusterConfiguration
    {
        private object _lock = new object();

        public ClusterConfiguration()
        {
        }

        public ConcurrentDictionary<string, INodeConfiguration> Peers { get; private set; }

        public INodeConfiguration ThisNode { get; private set; }

        public void UpdateConfiguration(string thisNodeId, IEnumerable<INodeConfiguration> allNodes)
        {
            var map = new ConcurrentDictionary<string, INodeConfiguration>();
            INodeConfiguration thisNode = null;

            foreach (var node in allNodes)
            {
                if (node.UniqueNodeId != thisNodeId)
                {
                    map.TryAdd(node.UniqueNodeId, node);
                }
                else
                {
                    thisNode = node;
                }
            }

            if (thisNode == null)
            {
                throw new InvalidOperationException($"{thisNodeId} not found in Cluster Configuration");
            }

            lock (_lock)
            {
                Peers = map;
                ThisNode = thisNode;
            }
        }
    }
}
