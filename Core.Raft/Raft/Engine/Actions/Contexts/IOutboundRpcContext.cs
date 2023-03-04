using Coracle.Raft.Engine.Configuration.Cluster;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal interface IOutboundRpcContext : IActionContext
    {
        INodeConfiguration NodeConfiguration { get; set; }
    }
}
