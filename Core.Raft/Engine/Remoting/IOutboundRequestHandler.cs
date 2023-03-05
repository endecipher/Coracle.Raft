using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Remoting
{
    public interface IOutboundRequestHandler
    {
        /// <summary>
        /// Invoked by Coracle for an outbound remote call <see cref="IAppendEntriesRPC"/>
        /// </summary>
        /// <param name="callObject"><see cref="IAppendEntriesRPC"/> object</param>
        /// <param name="configuration">Node Configuration towards which the call object needs to be sent</param>
        /// <param name="cancellationToken">A cancellation token in case Coracle Engine decides to cancel the operation</param>
        /// <returns>An awaitable operation with <see cref="RemoteCallResult{IAppendEntriesRPCResponse}"/> result encompassing the <see cref="IAppendEntriesRPCResponse"/> response object</returns>
        Task<RemoteCallResult<IAppendEntriesRPCResponse>> Send(IAppendEntriesRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked by Coracle for an outbound remote call <see cref="IRequestVoteRPC"/>
        /// </summary>
        /// <param name="callObject"><see cref="IRequestVoteRPC"/> object</param>
        /// <param name="configuration">Node Configuration towards which the call object needs to be sent</param>
        /// <param name="cancellationToken">A cancellation token in case Coracle Engine decides to cancel the operation</param>
        /// <returns>An awaitable operation with <see cref="RemoteCallResult{IRequestVoteRPCResponse}"/> result encompassing the <see cref="IRequestVoteRPCResponse"/> response object</returns>
        Task<RemoteCallResult<IRequestVoteRPCResponse>> Send(IRequestVoteRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked by Coracle for an outbound remote call <see cref="IInstallSnapshotRPCResponse"/>
        /// </summary>
        /// <param name="callObject"><see cref="IInstallSnapshotRPC"/> object</param>
        /// <param name="configuration">Node Configuration towards which the call object needs to be sent</param>
        /// <param name="cancellationToken">A cancellation token in case Coracle Engine decides to cancel the operation</param>
        /// <returns>An awaitable operation with <see cref="RemoteCallResult{IInstallSnapshotRPCResponse}"/> result encompassing the <see cref="IInstallSnapshotRPCResponse"/> response object</returns>
        Task<RemoteCallResult<IInstallSnapshotRPCResponse>> Send(IInstallSnapshotRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken);
    }
}
