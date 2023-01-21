using Coracle.Raft.Engine.Configuration.Cluster;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Discovery.Registrar
{
    /// <summary>
    /// Will be used by Registry Server to handle requests and alert nodes. 
    /// </summary>
    public interface INodeRegistrar
    {
        Task<IDiscoveryOperation> Enroll(NodeConfiguration configuration, CancellationToken cancellationToken);

        Task Clear();

        Task<IDiscoveryOperation> GetAllNodes(CancellationToken cancellationToken);
    }
}
