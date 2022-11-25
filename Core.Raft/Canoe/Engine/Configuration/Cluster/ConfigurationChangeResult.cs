using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Operational;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Configuration
{
    /// <summary>
    /// Instances of this class will hold the resultant expected TCommandResult and other information helpful with the operation.
    /// </summary>
    public sealed class ConfigurationChangeResult : OperationalResult
    {
        public ConfigurationChangeResult()
        {

        }
        public INodeConfiguration LeaderNodeConfiguration { get; set; }
        public IEnumerable<INodeChangeConfiguration> JointConsensusConfiguration { get; set; }
        public IEnumerable<INodeConfiguration> OriginalConfiguration { get; set; }
        public IEnumerable<INodeConfiguration> ConfigurationChangeRequest { get; set; }
    }
}
