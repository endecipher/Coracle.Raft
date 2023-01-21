using Coracle.Raft.Engine.ClientHandling.Command;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.ClientHandling
{
    /// <summary>
    /// External Client Command Handler. Transient in nature. 
    /// </summary>
    public interface IExternalClientCommandHandler
    {
        /// <summary>
        /// This method should be invoked when an external client command needs to be processed by Canoe Node 
        /// </summary>
        /// <typeparam name="TCommand">An extensible <see cref="AbstractCommand"/> concrete implementation</typeparam>
        /// <typeparam name="TCommandResult">An extensible Resultant class</typeparam>
        /// <param name="Command">Command to be processed</param>
        /// <param name="cancellationToken">Cancellation Token in case Canoe Engine is requested to cancel processing this command</param>
        /// <returns></returns>
        Task<ClientHandlingResult> HandleClientCommand<TCommand>(TCommand Command, CancellationToken cancellationToken)
            where TCommand : class, ICommand;
    }
}
