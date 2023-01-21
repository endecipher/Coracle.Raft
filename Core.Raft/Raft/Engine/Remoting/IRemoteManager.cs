using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting.RPC;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Remoting
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
    }
}
