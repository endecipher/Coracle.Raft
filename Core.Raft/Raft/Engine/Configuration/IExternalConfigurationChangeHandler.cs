using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Configuration
{
    /// <summary>
    /// Used by an external entity, to issue configuration changes post <see cref="ICanoeNode.InitializeConfiguration"/>.
    /// 
    /// The configuration change is issued when the <see cref="ICanoeNode.IsStarted"/> is <c>true</c>, 
    /// and the processing may occur when the current Node is an active <see cref="States.Leader"/> of the Cluster.
    /// </summary>
    internal interface IExternalConfigurationChangeHandler
    {
        Task<ConfigurationChangeResult> IssueConfigurationChange(ConfigurationChangeRPC configurationChangeRPC, CancellationToken cancellationToken);
    }
}
