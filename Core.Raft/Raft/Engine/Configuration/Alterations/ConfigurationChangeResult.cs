using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    public sealed class ConfigurationChangeResult : BaseOperationResult
    {
        public ConfigurationChangeResult()
        {

        }
        public INodeConfiguration LeaderNodeConfiguration { get; set; }
        public IEnumerable<NodeChangeConfiguration> JointConsensusConfiguration { get; set; }
        public IEnumerable<INodeConfiguration> OriginalConfiguration { get; set; }
        public IEnumerable<INodeConfiguration> ConfigurationChangeRequest { get; set; }
    }
}
