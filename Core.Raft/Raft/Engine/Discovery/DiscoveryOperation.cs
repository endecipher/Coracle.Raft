using Coracle.Raft.Engine.Configuration.Cluster;
using System;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Discovery
{
    public class DiscoveryOperation : IDiscoveryOperation
    {
        public IEnumerable<NodeConfiguration> AllNodes { get; set; }

        public bool IsOperationSuccessful { get; set; }

        public Exception Exception { get; set; }
    }
}
