using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Discovery
{
    public class DiscoveryOperation : IDiscoveryOperation
    {
        public IEnumerable<NodeConfiguration> AllNodes { get; set; }

        public bool IsOperationSuccessful { get; set; }

        public Exception Exception { get; set; }
    }
}
