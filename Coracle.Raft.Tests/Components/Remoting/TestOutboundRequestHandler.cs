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

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Tests.Framework;
using Coracle.Raft.Examples.Logging;

namespace Coracle.Raft.Tests.Components.Remoting
{
    public class TestOutboundRequestHandler : IOutboundRequestHandler
    {
        #region Constants
        public const string TestRemoteManagerEntity = nameof(TestOutboundRequestHandler);
        public const string Node = nameof(Node);
        public const string RPC = nameof(RPC);
        public const string OutboundRequestVoteRPC = nameof(OutboundRequestVoteRPC);
        public const string OutboundInstallSnapshotRPC = nameof(OutboundInstallSnapshotRPC);
        public const string OutboundAppendEntriesRPC = nameof(OutboundAppendEntriesRPC);
        #endregion

        public IMockNodeContext NodeContext { get; }
        public IActivityLogger ActivityLogger { get; }

        public TestOutboundRequestHandler(IMockNodeContext nodeContext, IActivityLogger activityLogger)
        {
            NodeContext = nodeContext;
            ActivityLogger = activityLogger;
        }

        public Task<RemoteCallResult<IAppendEntriesRPCResponse>> Send(IAppendEntriesRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            ActivityLogger.Log(new ImplActivity
            {
                EntitySubject = TestRemoteManagerEntity,
                Event = OutboundAppendEntriesRPC,
                Level = ActivityLogLevel.Debug
            }
            .With(ActivityParam.New(Node, configuration))
            .With(ActivityParam.New(RPC, callObject))
            .WithCallerInfo());

            var (response, exception) = NodeContext.GetMockNode(configuration.UniqueNodeId).AppendEntriesLock.WaitUntilResponse(callObject);

            if (exception != null)
                throw exception;

            var operationResult = new RemoteCallResult<IAppendEntriesRPCResponse>()
            {
                Response = response,
                Exception = null
            };

            return Task.FromResult(operationResult);
        }

        public Task<RemoteCallResult<IRequestVoteRPCResponse>> Send(IRequestVoteRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            ActivityLogger.Log(new ImplActivity
            {
                EntitySubject = TestRemoteManagerEntity,
                Event = OutboundRequestVoteRPC,
                Level = ActivityLogLevel.Debug
            }
            .With(ActivityParam.New(Node, configuration))
            .With(ActivityParam.New(RPC, callObject))
            .WithCallerInfo());

            var (response, exception) = NodeContext.GetMockNode(configuration.UniqueNodeId).RequestVoteLock.WaitUntilResponse(callObject);

            if (exception != null)
                throw exception;

            var operationResult = new RemoteCallResult<IRequestVoteRPCResponse>()
            {
                Response = response,
                Exception = null
            };

            return Task.FromResult(operationResult);
        }

        public Task<RemoteCallResult<IInstallSnapshotRPCResponse>> Send(IInstallSnapshotRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            ActivityLogger.Log(new ImplActivity
            {
                EntitySubject = TestRemoteManagerEntity,
                Event = OutboundInstallSnapshotRPC,
                Level = ActivityLogLevel.Debug
            }
            .With(ActivityParam.New(Node, configuration))
            .With(ActivityParam.New(RPC, callObject))
            .WithCallerInfo());

            var (response, exception) = NodeContext.GetMockNode(configuration.UniqueNodeId).InstallSnapshotLock.WaitUntilResponse(callObject);

            if (exception != null)
                throw exception;

            var operationResult = new RemoteCallResult<IInstallSnapshotRPCResponse>()
            {
                Response = response,
                Exception = null
            };

            return Task.FromResult(operationResult);
        }
    }
}
