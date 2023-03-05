using Coracle.Raft.Engine.Node;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConfigurationRequestExecutor
    {
        /// <summary>
        /// Used by an external entity, to issue configuration changes post <see cref="ICoracleNode.InitializeConfiguration"/>.
        /// 
        /// The configuration change is issued when the <see cref="ICoracleNode.IsStarted"/> is <c>true</c>, 
        /// and the processing may occur when the current Node is an active <see cref="States.Leader"/> of the Cluster.
        /// </summary>
        /// <param name="configurationChangeRequest">Change Request which denotes the target configuration of the cluster</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<ConfigurationChangeResult> IssueChange(ConfigurationChangeRequest configurationChangeRequest, CancellationToken cancellationToken);
    }
}
