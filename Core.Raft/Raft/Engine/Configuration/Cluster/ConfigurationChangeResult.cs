using Coracle.Raft.Engine.Operational;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Cluster
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
