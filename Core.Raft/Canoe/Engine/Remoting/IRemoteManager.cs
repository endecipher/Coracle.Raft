using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Remoting
{
    /// <summary>
    /// Interface for Extensible Remote Operations.
    /// Implementations of this class will need to handle outgoing RPCs requested by Canoe Engine.
    /// Transient in nature.
    /// </summary>
    public interface IRemoteManager
    {
        /// <summary>
        /// This method will be invoked by Canoe for a remote RPC operation for <see cref="IAppendEntriesRPC"/>
        /// </summary>
        /// <param name="callObject"><see cref="IAppendEntriesRPC"/> object</param>
        /// <param name="configuration">Node Configuration towards which the call object needs to be sent</param>
        /// <param name="cancellationToken">A cancellation token in case Canoe Engine decides to cancel the operation</param>
        /// <returns>An awaitable operation with <see cref="Operation{IAppendEntriesRPCResponse}"/> result encompassing the <see cref="IAppendEntriesRPCResponse"/> response object</returns>
        Task<Operation<IAppendEntriesRPCResponse>> Send(IAppendEntriesRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// This method will be invoked by Canoe for a remote RPC operation for <see cref="IRequestVoteRPC"/>
        /// </summary>
        /// <param name="callObject"><see cref="IRequestVoteRPC"/> object</param>
        /// <param name="configuration">Node Configuration towards which the call object needs to be sent</param>
        /// <param name="cancellationToken">A cancellation token in case Canoe Engine decides to cancel the operation</param>
        /// <returns>An awaitable operation with <see cref="Operation{IRequestVoteRPCResponse}"/> result encompassing the <see cref="IRequestVoteRPCResponse"/> response object</returns>
        Task<Operation<IRequestVoteRPCResponse>> Send(IRequestVoteRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Extension to forward the Client Command to the Leader.
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        Task<ClientHandlingResult> ForwardToLeader<TCommand>(TCommand command, INodeConfiguration configuration, CancellationToken cancellationToken) where TCommand : class, ICommand;
    }
}
