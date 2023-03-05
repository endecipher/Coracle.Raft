using Coracle.Raft.Engine.Configuration.Cluster;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    public class NodeChangeConfiguration : NodeConfiguration
    {
        public bool IsOld { get; set; }
        public bool IsNew { get; set; }
    }
}
