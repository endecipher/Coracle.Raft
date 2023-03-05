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
