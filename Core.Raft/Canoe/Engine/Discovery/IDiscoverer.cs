using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Discovery
{
    /// <summary>
    /// Implementation of this interface facilitates the storage and retrieval of all up and running Coracle Nodes.
    /// Usually, an external Registar server holds this responsibility. 
    /// However, it is possible for Coracle to adapt to a file-based configuration system if the implementation of this interface deals with configuration files.
    /// 
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

        /// <summary>
        /// The Service Discovery registar server should know that this Current Node is the Leader.
        /// In case there any configuration changes, the Discovery Server should send the updated Configuration to this Node back.
        /// </summary>
        /// <param name="registrarUri"></param>
        /// <param name="configuration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IDiscoveryOperation> NotifyCurrentNodeAsLeader(Uri registrarUri, INodeConfiguration configuration, CancellationToken cancellationToken);
    }
}