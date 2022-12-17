using System;

namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    public interface INodeConfiguration 
    {
        public string UniqueNodeId { get; }

        public Uri BaseUri { get; }
    }
}
