using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Discovery
{
    /// <summary>
    /// <see cref="IDiscoveryHandler"/> holds the methods, used by the <see cref="CoracleNode"/> internally, to get the list of <see cref="INodeConfiguration"/>, 
    /// participating in the cluster. 
    /// 
    /// The storage and retrieval of all up nodes could be maintained via configuration file based systems (or) a registry server.
    /// 
    /// The methods used, are called only when <see cref="ICoracleNode.IsStarted"/> is <c>false</c> i.e. during initialization.
    /// </summary>
    public interface IDiscoveryHandler
    {
        /// <summary>
        /// Enrolls current node for Discovery. 
        /// For File-based configurations, it may not be necessary.
        /// </summary>
        /// <returns>A wrapper <see cref="DiscoveryResult"/> detailing over the discovery operation</returns>
        Task<DiscoveryResult> Enroll(INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches all node information from the File-based system (or) the Registry Server (if Service Discovery is implemented). 
        /// </summary>
        /// <returns>
        /// A wrapper <see cref="DiscoveryResult"/> detailing over the discovery operation. 
        /// The <see cref="DiscoveryResult.AllNodes"/> would contain information about the operational nodes of the cluster post successful discovery
        /// </returns>
        Task<DiscoveryResult> GetAllNodes(CancellationToken cancellationToken);
    }
}