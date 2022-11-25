using Core.Raft.Canoe.Engine.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Discovery
{
    internal interface IExternalConfigurationChangeHandler
    {
        Task<ConfigurationChangeResult> IssueConfigurationChange(ConfigurationChangeRPC configurationChangeRPC, CancellationToken cancellationToken);
    }
}
