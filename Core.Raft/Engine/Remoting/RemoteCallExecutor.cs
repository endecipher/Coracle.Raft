using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Configuration.Alterations;

namespace Coracle.Raft.Engine.Remoting
{
    internal sealed class RemoteCallExecutor : IRemoteCallExecutor
    {
        public RemoteCallExecutor(
            IActivityLogger activityLogger,
            IResponsibilities responsibilities,
            ICurrentStateAccessor currentStateAccessor,
            IEngineConfiguration engineConfiguration,
            IPersistentStateHandler persistentState,
            ILeaderNodePronouncer leaderNodePronouncer,
            IMembershipChanger clusterConfigurationChanger)
        {
            ActivityLogger = activityLogger;
            Responsibilities = responsibilities;
            CurrentStateAccessor = currentStateAccessor;
            EngineConfiguration = engineConfiguration;
            PersistentState = persistentState;
            LeaderNodePronouncer = leaderNodePronouncer;
            ClusterConfigurationChanger = clusterConfigurationChanger;
        }

        IActivityLogger ActivityLogger { get; }
        IResponsibilities Responsibilities { get; }
        public ICurrentStateAccessor CurrentStateAccessor { get; }
        public IEngineConfiguration EngineConfiguration { get; }
        public IPersistentStateHandler PersistentState { get; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; }
        public IMembershipChanger ClusterConfigurationChanger { get; }

        public Task<RemoteCallResult<IAppendEntriesRPCResponse>> RespondTo(IAppendEntriesRPC externalRequest, CancellationToken cancellationToken)
        {
            RemoteCallResult<IAppendEntriesRPCResponse> response = new RemoteCallResult<IAppendEntriesRPCResponse>();

            try
            {
                var action = new OnExternalAppendEntriesRPCReceive(externalRequest as AppendEntriesRPC, CurrentStateAccessor.Get(), new Actions.Contexts.OnExternalRPCReceiveContextDependencies
                {
                    EngineConfiguration = EngineConfiguration,
                    PersistentState = PersistentState,
                    LeaderNodePronouncer = LeaderNodePronouncer,
                    ClusterConfigurationChanger = ClusterConfigurationChanger,

                }, ActivityLogger);

                action.SupportCancellation();

                action.CancellationManager.Bind(cancellationToken);

                response.Response = Responsibilities.QueueBlockingAction<AppendEntriesRPCResponse>(
                    action: action, executeSeparately: false
                );
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return Task.FromResult(response);
        }

        public Task<RemoteCallResult<IRequestVoteRPCResponse>> RespondTo(IRequestVoteRPC externalRequest, CancellationToken cancellationToken)
        {
            RemoteCallResult<IRequestVoteRPCResponse> response = new RemoteCallResult<IRequestVoteRPCResponse>();

            try
            {
                var action = new OnExternalRequestVoteRPCReceive(externalRequest as RequestVoteRPC, CurrentStateAccessor.Get(), new Actions.Contexts.OnExternalRPCReceiveContextDependencies
                {
                    EngineConfiguration = EngineConfiguration,
                    PersistentState = PersistentState,
                    LeaderNodePronouncer = LeaderNodePronouncer,
                    ClusterConfigurationChanger = ClusterConfigurationChanger

                }, ActivityLogger);

                action.SupportCancellation();

                action.CancellationManager.Bind(cancellationToken);

                response.Response = Responsibilities.QueueBlockingAction<RequestVoteRPCResponse>(
                    action: action, executeSeparately: false
                );
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return Task.FromResult(response);
        }

        public Task<RemoteCallResult<IInstallSnapshotRPCResponse>> RespondTo(IInstallSnapshotRPC externalRequest, CancellationToken cancellationToken)
        {
            RemoteCallResult<IInstallSnapshotRPCResponse> response = new RemoteCallResult<IInstallSnapshotRPCResponse>();

            try
            {
                var action = new OnExternalInstallSnapshotChunkRPCReceive(externalRequest as InstallSnapshotRPC, CurrentStateAccessor.Get(), new Actions.Contexts.OnExternalRPCReceiveContextDependencies
                {
                    EngineConfiguration = EngineConfiguration,
                    PersistentState = PersistentState,
                    LeaderNodePronouncer = LeaderNodePronouncer,
                    ClusterConfigurationChanger = ClusterConfigurationChanger,

                }, ActivityLogger);

                action.SupportCancellation();

                action.CancellationManager.Bind(cancellationToken);

                response.Response = Responsibilities.QueueBlockingAction<InstallSnapshotRPCResponse>(
                    action: action, executeSeparately: false
                );
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return Task.FromResult(response);
        }
    }
}
