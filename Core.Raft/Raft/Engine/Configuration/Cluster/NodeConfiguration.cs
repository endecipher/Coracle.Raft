using System;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    public class NodeConfiguration : INodeConfiguration
    {
        public string UniqueNodeId { get; set; }

        public Uri BaseUri { get; set; } = null;
    }
}
