using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    public class MembershipUpdateEvent
    {
        public IEnumerable<NodeConfiguration> Configuration { get; set; }

        public long ConfigurationLogEntryIndex { get; set; }
    }
}
