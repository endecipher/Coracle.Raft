using Coracle.Raft.Engine.ClientHandling.Command;

namespace Coracle.Web.Controllers
{
    public interface ICoracleClient
    {
        Task<string> ExecuteCommand(ICommand command, CancellationToken token);
    }
}
