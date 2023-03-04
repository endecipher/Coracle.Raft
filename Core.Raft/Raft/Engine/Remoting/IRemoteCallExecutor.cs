using Coracle.Raft.Engine.Remoting.RPC;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Remoting
{
    public interface IRemoteCallExecutor
    {
        /// <summary>
        /// Invoked when an external <see cref="IAppendEntriesRPC"/> call has been recognized towards this Coracle Node
        /// </summary>
        /// <param name="externalRequest">Received <see cref="IAppendEntriesRPC"/> object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An awaitable operation with <see cref="RemoteCallResult{IAppendEntriesRPCResponse}"/> result encompassing Coracle Node's reply, or the <see cref="IAppendEntriesRPCResponse"/> response object to send back</returns>
        Task<RemoteCallResult<IAppendEntriesRPCResponse>> RespondTo(IAppendEntriesRPC externalRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when an external <see cref="IRequestVoteRPC"/> call has been recognized towards this Coracle Node
        /// </summary>
        /// <param name="externalRequest">Received <see cref="IRequestVoteRPC"/> object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An awaitable operation with <see cref="RemoteCallResult{IRequestVoteRPCResponse}"/> result encompassing Coracle Node's reply, or the <see cref="IRequestVoteRPCResponse"/> response object to send back</returns>
        Task<RemoteCallResult<IRequestVoteRPCResponse>> RespondTo(IRequestVoteRPC externalRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when an external <see cref="IInstallSnapshotRPC"/> call has been recognized towards this Coracle Node
        /// </summary>
        /// <param name="externalRequest">Received <see cref="IInstallSnapshotRPC"/> object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An awaitable operation with <see cref="RemoteCallResult{IInstallSnapshotRPCResponse}"/> result encompassing Coracle Node's reply, or the <see cref="IInstallSnapshotRPCResponse"/> response object to send back</returns>
        Task<RemoteCallResult<IInstallSnapshotRPCResponse>> RespondTo(IInstallSnapshotRPC externalRequest, CancellationToken cancellationToken);
    }
}
