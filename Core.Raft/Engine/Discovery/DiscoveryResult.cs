using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Discovery
{
    public class DiscoveryResult : BaseOperationResult
    {
        public IEnumerable<NodeConfiguration> AllNodes { get; set; }
    }
}
