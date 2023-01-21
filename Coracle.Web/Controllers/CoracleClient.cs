using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Exceptions;

namespace Coracle.Web.Controllers
{
    public class CoracleClient : ICoracleClient
    {
        public CoracleClient(IExternalClientCommandHandler externalClientCommandHandler)
        {
            ExternalClientCommandHandler = externalClientCommandHandler;
        }

        public IExternalClientCommandHandler ExternalClientCommandHandler { get; }

        public async Task<string> ExecuteCommand(ICommand command, CancellationToken token)
        {
            var result = await ExternalClientCommandHandler.HandleClientCommand(command, token);

            if (result.IsOperationSuccessful)
            {
                return result.CommandResult.ToString();
            }
            else if (result.Exception != null && result.Exception is ClientCommandDeniedException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return await ExecuteCommand(command, token);
            }
            else 
            {
                return result.Exception.Message;
            }
        }
    }
}
