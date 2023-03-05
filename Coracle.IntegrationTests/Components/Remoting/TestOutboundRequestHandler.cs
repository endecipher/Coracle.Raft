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
