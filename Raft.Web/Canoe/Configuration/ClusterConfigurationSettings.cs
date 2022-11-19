using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Raft.Web.Controllers;
using System.Collections.Concurrent;

namespace Raft.Web.Canoe.Configuration
{
    public sealed class ClusterConfigurationSettings : IClusterConfiguration
    {
        public ClusterConfigurationSettings()
        {
            Peers = new ConcurrentDictionary<string, INodeConfiguration>();
        }

        public ConcurrentDictionary<string, INodeConfiguration> Peers { get; }

        public INodeConfiguration ThisNode { get; } 

        public void AddPeer(INodeConfiguration nodeConfiguration)
        {
            Peers.TryAdd(nodeConfiguration.UniqueNodeId, nodeConfiguration);
        }

        public INodeConfiguration GenerateRandomNode()
        {
            return new NodeConfiguration
            {
                UniqueNodeId = $"_Node{Peers.Count + 1}_",
            };
        }
    }

    public sealed class NodeConfiguration : INodeConfiguration
    {
        public string UniqueNodeId { get; init; }

        public Uri BaseUri { get; set; }

        public string AppendEntriesEndpoint { get; set; } = Routes.Endpoints.AppendEntries;

        public string RequestVoteEndpoint { get; set; } = Routes.Endpoints.RequestVote;

        public string CommandHandlingEndpoint { get; set; } = Routes.Endpoints.HandleClientCommand;
    }
}
