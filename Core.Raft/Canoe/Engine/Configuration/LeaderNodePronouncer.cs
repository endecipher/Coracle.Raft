using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using System;

namespace Core.Raft.Canoe.Engine.Configuration
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
            if (ClusterConfiguration.Peers.TryGetValue(leaderServerId, out var configuration))
            {
                RecognizedLeaderConfiguration = configuration;
                return;
            }

            throw new ArgumentException($"{nameof(leaderServerId)} {leaderServerId} does not exist");
        }

        public void SetRunningNodeAsLeader()
        {
            RecognizedLeaderConfiguration = ClusterConfiguration.ThisNode;

            //TODO: Configuration Change: Discoverer.
        }
    }
}
