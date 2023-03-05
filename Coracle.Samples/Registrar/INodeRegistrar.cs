using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;

namespace Coracle.Raft.Examples.Registrar
{
    public interface INodeRegistrar
    {
        Task<DiscoveryResult> Enroll(NodeConfiguration configuration, CancellationToken cancellationToken);
        Task Clear();
        Task<DiscoveryResult> GetAllNodes(CancellationToken cancellationToken);
    }
}
