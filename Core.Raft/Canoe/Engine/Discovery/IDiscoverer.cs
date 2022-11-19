using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Discovery
{
    /// <summary>
    /// Transient in nature
    /// </summary>
    public interface IDiscoverer
    {
        /// <summary>
        /// Enrolls current node to Node Registry Server for Service Discovery. 
        /// <see cref="Core.Raft.Canoe.Engine.Discovery.Registrar.INodeRegistrar"/> to be implemented by Registry Server
        /// </summary>
        Task<IDiscoveryOperation> EnrollThisNode(Uri registrarUri, INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Gets All Node info from Node Registry Server for Service Discovery. 
        /// <see cref="Core.Raft.Canoe.Engine.Discovery.Registrar.INodeRegistrar"/> to be implemented by Registry Server
        /// </summary>
        Task<IDiscoveryOperation> GetAllNodes(Uri registrarUri, CancellationToken cancellationToken);

        /// <summary>
        /// Usually called explicitly, or by the Registry Server to indicate an updated configuration 
        /// </summary>
        Task RefreshDiscovery();
    }
}