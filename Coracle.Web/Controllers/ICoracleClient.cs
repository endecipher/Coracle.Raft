using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Samples.ClientHandling.NoteCommand;

namespace Coracle.Web.Controllers
{
    public interface ICoracleClient
    {
        Task<string> ExecuteCommand(NoteCommand command, CancellationToken token);
        Task<string> ChangeConfiguration(ConfigurationChangeRPC changeRPC, CancellationToken token);
    }
}
