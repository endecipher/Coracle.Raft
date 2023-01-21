using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Raft.Engine.Remoting
{
    /// <summary>
    /// Called within a Transient Scope, to handle requests from external modules
    /// </summary>
    internal sealed class ExternalRemoteProcedureHandler : IExternalRpcHandler
    {
        public ExternalRemoteProcedureHandler(
            IActivityLogger activityLogger,
            IResponsibilities responsibilities,
            ICurrentStateAccessor currentStateAccessor,
            IEngineConfiguration engineConfiguration,
            IPersistentProperties persistentState,
            ILeaderNodePronouncer leaderNodePronouncer,
            IClusterConfigurationChanger clusterConfigurationChanger)
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
        public IPersistentProperties PersistentState { get; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; }
        public IClusterConfigurationChanger ClusterConfigurationChanger { get; }

        public Task<Operation<IAppendEntriesRPCResponse>> RespondTo(IAppendEntriesRPC externalRequest, CancellationToken cancellationToken)
        {
            Operation<IAppendEntriesRPCResponse> response = new Operation<IAppendEntriesRPCResponse>();

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

        public Task<Operation<IRequestVoteRPCResponse>> RespondTo(IRequestVoteRPC externalRequest, CancellationToken cancellationToken)
        {
            Operation<IRequestVoteRPCResponse> response = new Operation<IRequestVoteRPCResponse>();

            try
            {
                var action = new OnExternalRequestVoteRPCReceive(externalRequest as RequestVoteRPC, CurrentStateAccessor.Get(), new Actions.Contexts.OnExternalRPCReceiveContextDependencies
                {
                    EngineConfiguration = EngineConfiguration,
                    PersistentState = PersistentState,
                    LeaderNodePronouncer = LeaderNodePronouncer
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
    }
}
