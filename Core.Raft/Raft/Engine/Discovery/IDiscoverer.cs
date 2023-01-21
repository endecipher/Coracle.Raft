using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Discovery
{
    /// <summary>
    /// <see cref="IDiscoverer"/> holds the methods, used by the <see cref="CanoeNode"/> internally, to get the list of <see cref="INodeConfiguration"/>, 
    /// participating in the cluster. 
    /// 
    /// The storage and retrieval of all up nodes could be maintained via configuration file based systems (or) a registry server.
    /// 
    /// The methods used, are called only when <see cref="ICanoeNode.IsStarted"/> is <c>false</c>.
    /// </summary>
    public interface IDiscoverer
    {
        /// <summary>
        /// Enrolls current node for Discovery. 
        /// 
        /// <see cref="Registrar.INodeRegistrar"/> can help if implemented by Registry Server. 
        /// For File-based configurations, it may not be necessary.
        /// </summary>
        Task<IDiscoveryOperation> EnrollThisNode(Uri registrarUri, INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches all node information from the File-based system (or) the Registry Server (if Service Discovery is implemented) 
        /// 
        /// <see cref="Registrar.INodeRegistrar"/> can help if implemented by Registry Server. 
        /// </summary>
        Task<IDiscoveryOperation> GetAllNodes(Uri registrarUri, CancellationToken cancellationToken);
    }
}