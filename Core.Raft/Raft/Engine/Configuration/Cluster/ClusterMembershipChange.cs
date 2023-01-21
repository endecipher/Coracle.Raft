using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    public class ClusterMembershipChange
    {
        public IEnumerable<NodeConfiguration> Configuration { get; set; }

        public long ConfigurationLogEntryIndex { get; set; }
    }
}
