using System;

namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    public class NodeConfiguration : INodeConfiguration
    {
        public string UniqueNodeId { get; set; }

        public Uri BaseUri { get; set; } = null;

        public object Clone()
        {
            return new NodeConfiguration
            {
                UniqueNodeId = UniqueNodeId,
                BaseUri = new UriBuilder(BaseUri).Uri
            };
        }
    }
}
