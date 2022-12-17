using Core.Raft.Canoe.Engine.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Discovery
{
    /// <summary>
    /// Used by an external entity, to issue configuration changes post <see cref="Canoe.Engine.Node.ICanoeNode.InitializeConfiguration"/>.
    /// 
    /// The configuration change is issued when the <see cref="Canoe.Engine.Node.ICanoeNode.IsStarted"/> is <c>true</c>, 
    /// and the processing may occur when the current Node is an active <see cref="Canoe.Engine.States.Leader"/> of the Cluster.
    /// </summary>
    internal interface IExternalConfigurationChangeHandler
    {
        Task<ConfigurationChangeResult> IssueConfigurationChange(ConfigurationChangeRPC configurationChangeRPC, CancellationToken cancellationToken);
    }
}
