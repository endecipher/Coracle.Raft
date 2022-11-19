using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Discovery.Registrar
{
    /// <summary>
    /// Will be used by Registry Server to handle requests and alert nodes. 
    /// </summary>
    public interface INodeRegistrar
    {
        Task<IDiscoveryOperation> Enroll(NodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// This will be called post a successful <see cref="Enroll(INodeConfiguration, CancellationToken)"/>.
        /// Extensible remote calls will be sent to alert all registered Nodes, so that they can trigger <see cref="IDiscoverer.RefreshDiscovery"/>
        /// </summary>
        Task AlertAllNodesForRefresh(CancellationToken cancellationToken);

        Task<IDiscoveryOperation> GetAllNodes(CancellationToken cancellationToken);
    }

}
