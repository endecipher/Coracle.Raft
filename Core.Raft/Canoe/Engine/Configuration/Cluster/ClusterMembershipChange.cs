using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    public class ClusterMembershipChange
    {
        public IEnumerable<NodeConfiguration> Configuration { get; set; }

        public long ConfigurationLogEntryIndex { get; set; }
    }
}
