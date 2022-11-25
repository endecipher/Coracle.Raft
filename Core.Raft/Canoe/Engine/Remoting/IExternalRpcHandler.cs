﻿using Core.Raft.Canoe.Engine.Operational;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Remoting
{
    /// <summary>
    /// External RPC Handler to be invoked when RPC calls are requested towards this Canoe Node. Transient in nature. 
    /// </summary>
    public interface IExternalRpcHandler
    {
        /// <summary>
        /// This method should be invoked when an external <see cref="IAppendEntriesRPC"/> call has been recognized towards the associated Canoe Node
        /// </summary>
        /// <param name="externalRequest"><see cref="IAppendEntriesRPC"/> object</param>
        /// <param name="cancellationToken">A cancellation token in case Canoe needs to cancel ongoing processing assocaited to <paramref name="externalRequest"/></param>
        /// <returns>An awaitable operation with <see cref="Operation{IAppendEntriesRPCResponse}"/> result encompassing Canoe Node's reply, or the <see cref="IAppendEntriesRPCResponse"/> response object</returns>
        Task<Operation<IAppendEntriesRPCResponse>> RespondTo(IAppendEntriesRPC externalRequest, CancellationToken cancellationToken);

        /// <summary>
        /// This method should be invoked when an external <see cref="IRequestVoteRPC"/> call has been recognized towards the associated Canoe Node
        /// </summary>
        /// <param name="externalRequest"><see cref="IRequestVoteRPC"/> object</param>
        /// <param name="cancellationToken">A cancellation token in case Canoe needs to cancel ongoing processing assocaited to <paramref name="externalRequest"/></param>
        /// <returns>An awaitable operation with <see cref="Operation{IRequestVoteRPCResponse}"/> result encompassing Canoe Node's reply, or the <see cref="IRequestVoteRPCResponse"/> response object</returns>
        Task<Operation<IRequestVoteRPCResponse>> RespondTo(IRequestVoteRPC externalRequest, CancellationToken cancellationToken);
    }
}
