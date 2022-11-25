using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Configuration
{
    public class ConfigurationChangeRPC
    {
        public string UniqueId { get; init; }
        public IEnumerable<NodeConfiguration> NewConfiguration { get; init; }
    }
}
