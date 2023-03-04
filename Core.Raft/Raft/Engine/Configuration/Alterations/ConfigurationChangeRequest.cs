using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    public class ConfigurationChangeRequest
    {
        public string UniqueRequestId { get; init; }
        public IEnumerable<NodeConfiguration> NewConfiguration { get; init; }
    }
}
