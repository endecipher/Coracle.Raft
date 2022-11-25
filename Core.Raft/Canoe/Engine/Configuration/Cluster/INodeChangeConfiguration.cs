namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    public interface INodeChangeConfiguration : INodeConfiguration
    {
        bool IsOld { get; set; }
        bool IsNew { get; set; }
    }
}
