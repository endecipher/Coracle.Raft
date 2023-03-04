using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Command
{
    public interface ICommandExecutor
    {
        /// <summary>
        /// To be invoked when an external client command needs to be processed 
        /// </summary>
        /// <typeparam name="TCommand">Extensible Command Type</typeparam>
        /// <param name="Command">Command to be processed</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        Task<CommandExecutionResult> Execute<TCommand>(TCommand Command, CancellationToken cancellationToken)
            where TCommand : class, ICommand;
    }
}
