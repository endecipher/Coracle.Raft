namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    public class NodeChangeConfiguration : NodeConfiguration, INodeChangeConfiguration
    {
        public bool IsOld { get; set; }
        public bool IsNew { get; set; }
    }
}
