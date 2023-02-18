using ActivityLogger.Logging;
using Coracle.IntegrationTests.Framework;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Samples.Logging;

namespace Coracle.IntegrationTests.Components.Remoting
{
    public class TestRemoteManager : IRemoteManager
    {
        #region Constants
        public const string TestRemoteManagerEntity = nameof(TestRemoteManager);
        public const string Node = nameof(Node);
        public const string RPC = nameof(RPC);
        public const string OutboundRequestVoteRPC = nameof(OutboundRequestVoteRPC);
        public const string OutboundInstallSnapshotRPC = nameof(OutboundInstallSnapshotRPC);
        public const string OutboundAppendEntriesRPC = nameof(OutboundAppendEntriesRPC);
        #endregion

        public INodeContext NodeContext { get; }
        public IActivityLogger ActivityLogger { get; }

        public TestRemoteManager(INodeContext nodeContext, IActivityLogger activityLogger)
        {
            NodeContext = nodeContext;
            ActivityLogger = activityLogger;
        }

        public Task<Operation<IAppendEntriesRPCResponse>> Send(IAppendEntriesRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
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

            var operationResult = new Operation<IAppendEntriesRPCResponse>()
            {
                Response = response,
                Exception = null
            };

            return Task.FromResult(operationResult);
        }

        public Task<Operation<IRequestVoteRPCResponse>> Send(IRequestVoteRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
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

            var operationResult = new Operation<IRequestVoteRPCResponse>()
            {
                Response = response,
                Exception = null
            };

            return Task.FromResult(operationResult);
        }

        public Task<Operation<IInstallSnapshotRPCResponse>> Send(IInstallSnapshotRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
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

            var operationResult = new Operation<IInstallSnapshotRPCResponse>()
            {
                Response = response,
                Exception = null
            };

            return Task.FromResult(operationResult);
        }
    }
}
