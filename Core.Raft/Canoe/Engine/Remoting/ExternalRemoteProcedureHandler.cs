using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using EventGuidance.Responsibilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Remoting
{
    /// <summary>
    /// Called within a Transient Scope, to handle requests from external modules
    /// </summary>
    internal sealed class ExternalRemoteProcedureHandler : IExternalRpcHandler
    {
        public ExternalRemoteProcedureHandler(IActivityLogger activityLogger, IResponsibilities responsibilities, ICurrentStateAccessor currentStateAccessor, IEngineConfiguration engineConfiguration, IPersistentProperties persistentState, ILeaderNodePronouncer leaderNodePronouncer)
        {
            ActivityLogger = activityLogger;
            Responsibilities = responsibilities;
            CurrentStateAccessor = currentStateAccessor;
            EngineConfiguration = engineConfiguration;
            PersistentState = persistentState;
            LeaderNodePronouncer = leaderNodePronouncer;
        }

        IActivityLogger ActivityLogger { get; }
        IResponsibilities Responsibilities { get; }
        public ICurrentStateAccessor CurrentStateAccessor { get; }
        public IEngineConfiguration EngineConfiguration { get; }
        public IPersistentProperties PersistentState { get; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; }

        public ExternalRemoteProcedureHandler()
        {

        }

        public Task<Operation<IAppendEntriesRPCResponse>> RespondTo(IAppendEntriesRPC externalRequest, CancellationToken cancellationToken)
        {
            Operation<IAppendEntriesRPCResponse> response = new Operation<IAppendEntriesRPCResponse>();

            try
            {
                var action = new OnExternalAppendEntriesRPCReceive(externalRequest as AppendEntriesRPC, CurrentStateAccessor.Get(), new Actions.Contexts.OnExternalRPCReceiveContextDependencies
                {
                    EngineConfiguration = EngineConfiguration,
                    PersistentState = PersistentState,
                    LeaderNodePronouncer = LeaderNodePronouncer
                }, ActivityLogger);

                action.SupportCancellation();

                action.CancellationManager.Bind(cancellationToken);

                response.Response = Responsibilities.QueueBlockingEventAction<AppendEntriesRPCResponse>(
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

                response.Response = Responsibilities.QueueBlockingEventAction<RequestVoteRPCResponse>(
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
