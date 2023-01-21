using Coracle.Raft.Engine.ClientHandling.Command;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.ClientHandling
{
    /// <remarks>
    /// If the leader
    /// crashes after committing the log entry but before responding to the client, the client will retry the command with a
    /// new leader, causing it to be executed a second time.The
    /// solution is for clients to assign unique serial numbers to
    /// every command.Then, the state machine tracks the latest
    /// serial number processed for each client, along with the associated response. If it receives a command whose serial
    /// number has already been executed, it responds immediately without re-executing the request.
    /// 
    /// <seealso cref="Section 8 Client Interaction"/>
    /// </remarks>
    public interface IClientRequestHandler
    {
        /// <remarks>
        /// ..so far Raft can execute a command multiple times:
        /// 
        /// If the leader crashes after committing the log entry but before responding to the client, the client will retry the command with a
        /// new leader, causing it to be executed a second time. The solution is for clients to assign unique serial numbers to
        /// every command. Then, the state machine tracks the latest serial number processed for each client, along with the associated response.
        /// If it receives a command whose serial number has already been executed, it responds immediately without re - executing the request.
        /// 
        /// <seealso cref="Section 8 Client Interaction"/>
        /// </remarks>
        Task<bool> IsCommandLatest(string uniqueCommandId, out ClientHandlingResult executedResult);

        /// <summary>
        /// Extension to check if the Client Command was already executed previously and return it.
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <typeparam name="TCommandResult"></typeparam>
        /// <param name="Command"></param>
        /// <returns>
        /// true if Command Already Executed
        /// false if not Executed
        /// </returns>
        Task<bool> TryGetCommandResult<TCommand>(TCommand Command, out ClientHandlingResult result) where TCommand : class, ICommand;

        /// <summary>
        /// Actual Execution of Command via a persisted Log Entry during the Commit Index update.
        /// If there are any exceptions during log entry application/in general, the Canoe Engine will not reinitiate application for this log Entry.
        /// The overridden logic should enforce/keep track of the order of execution of all future log entries to be applied, if there were log entries skipped to be applied previously.
        /// Canoe Engine will assume eveything worked fine, and the log Entry is applied successfully. 
        /// Any exceptions thrown by this method, will simply be logged and ignored.
        /// </summary>
        /// <param name="logEntryCommand"></param>
        /// <returns></returns>
        Task ExecuteAndApplyLogEntry(ICommand logEntryCommand);

    }
}
