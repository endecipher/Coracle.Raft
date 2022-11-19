using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System.Collections.Concurrent;

namespace Raft.Web.Canoe.Configuration
{
    public class SettingOptions : IClusterConfiguration
    {
        public ConcurrentDictionary<string, INodeConfiguration> Peers { get; set; }

        public INodeConfiguration ThisNode { get; set; }
    }
}
