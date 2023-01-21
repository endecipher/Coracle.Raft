using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Concurrent;

namespace Coracle.Web.Impl.Configuration
{
    public class SettingOptions
    {
        public ConcurrentDictionary<string, INodeConfiguration> Peers { get; set; }

        public INodeConfiguration ThisNode { get; set; }
    }
}
