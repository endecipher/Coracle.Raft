using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Raft.Engine.ClientHandling
{
    /// <summary>
    /// Command Handler. Transient in nature.
    /// </summary>
    internal sealed class ExternalClientCommandHandler : IExternalClientCommandHandler
    {
        public ExternalClientCommandHandler(
            IClientRequestHandler clientRequestHandler,
            IEngineConfiguration engineConfiguration,
            ILeaderNodePronouncer leaderNodePronouncer,
            ICurrentStateAccessor currentStateAccessor,
            IResponsibilities responsibilities,
            IGlobalAwaiter globalAwaiter,
            IPersistentProperties persistentState,
            IActivityLogger activityLogger,
            IAppendEntriesManager appendEntriesManager)
        {
            RequestHandler = clientRequestHandler;
            EngineConfiguration = engineConfiguration;
            LeaderNodePronouncer = leaderNodePronouncer;
            GlobalAwaiter = globalAwaiter;
            PersistentState = persistentState;
            ActivityLogger = activityLogger;
            AppendEntriesManager = appendEntriesManager;
            CurrentStateAccessor = currentStateAccessor;
            Responsibilities = responsibilities;
        }

        IClientRequestHandler RequestHandler { get; }
        IEngineConfiguration EngineConfiguration { get; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        IPersistentProperties PersistentState { get; }
        IActivityLogger ActivityLogger { get; }
        IAppendEntriesManager AppendEntriesManager { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IResponsibilities Responsibilities { get; }

        public Task<ClientHandlingResult> HandleClientCommand<TCommand>(TCommand Command, CancellationToken cancellationToken) where TCommand : class, ICommand
        {
            ClientHandlingResult result;

            var action = new OnClientCommandReceive<TCommand>(Command, CurrentStateAccessor.Get(), new Actions.Contexts.OnClientCommandReceiveContextDependencies
            {
                EngineConfiguration = EngineConfiguration,
                ClientRequestHandler = RequestHandler,
                LeaderNodePronouncer = LeaderNodePronouncer,
                GlobalAwaiter = GlobalAwaiter,
                PersistentState = PersistentState,
                AppendEntriesManager = AppendEntriesManager
            }, ActivityLogger);

            action.SupportCancellation();

            action.CancellationManager.Bind(cancellationToken);

            try
            {
                result = Responsibilities.QueueBlockingAction<ClientHandlingResult>(
                            action: action,
                            executeSeparately: false
                        );
            }
            catch (Exception e)
            {
                result = new ClientHandlingResult
                {
                    IsOperationSuccessful = false,
                    CommandResult = null,
                    OriginalCommand = EngineConfiguration.IncludeOriginalClientCommandInResults ? Command : null,
                    Exception = e,
                    LeaderNodeConfiguration = LeaderNodePronouncer.RecognizedLeaderConfiguration
                };
            }

            return Task.FromResult(result);
        }
    }
}
