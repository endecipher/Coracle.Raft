using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    public class ConfigurationChangeRPC
    {
        public string UniqueId { get; init; }
        public IEnumerable<NodeConfiguration> NewConfiguration { get; init; }
    }
}
