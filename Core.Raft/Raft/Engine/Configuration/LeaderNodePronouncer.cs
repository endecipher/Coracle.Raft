using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using System;

namespace Coracle.Raft.Engine.Configuration
{
    internal sealed class LeaderNodePronouncer : ILeaderNodePronouncer
    {
        public LeaderNodePronouncer(IClusterConfiguration clusterConfiguration, IDiscoverer discoverer)
        {
            ClusterConfiguration = clusterConfiguration;
            Discoverer = discoverer;
        }

        public INodeConfiguration RecognizedLeaderConfiguration { get; private set; } = null;
        public IClusterConfiguration ClusterConfiguration { get; }
        public IDiscoverer Discoverer { get; }

        public void SetNewLeader(string leaderServerId)
        {
            var configuration = ClusterConfiguration.GetPeerNodeConfiguration(leaderServerId);

            if (configuration != null)
            {
                RecognizedLeaderConfiguration = configuration;
                return;
            }

            throw new ArgumentException($"{nameof(leaderServerId)} {leaderServerId} does not exist");
        }

        public void SetRunningNodeAsLeader()
        {
            if (!ClusterConfiguration.IsThisNodePartOfCluster)
                throw new InvalidOperationException($"Cannot recognize leader if not part of the cluster"); //Impossible scenario

            RecognizedLeaderConfiguration = ClusterConfiguration.ThisNode;

            //TODO: Configuration Change: Discoverer.
        }
    }
}
