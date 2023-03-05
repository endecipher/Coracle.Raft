#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

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
