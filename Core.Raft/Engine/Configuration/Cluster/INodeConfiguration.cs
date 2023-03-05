using System;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    public interface INodeConfiguration
    {
        public string UniqueNodeId { get; }

        public Uri BaseUri { get; }
    }
}
