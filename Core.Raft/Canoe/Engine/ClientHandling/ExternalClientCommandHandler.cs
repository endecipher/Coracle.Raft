using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Canoe.Engine.States.LeaderState;
using EventGuidance.Responsibilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.ClientHandling
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

        public async Task<ClientHandlingResult> HandleClientCommand<TCommand>(TCommand Command, CancellationToken cancellationToken) where TCommand : class, ICommand
        {
            /// <remarks>
            /// If the leader crashes after committing the log entry but before responding to the client, the client will retry the command with a
            /// new leader, causing it to be executed a second time. 
            /// The solution is for clients to assign unique serial numbers to
            /// every command. Then, the state machine tracks the latest
            /// serial number processed for each client, along with the associated response.If it receives a command whose serial
            /// number has already been executed, it responds immediately without re - executing the request.
            /// <see cref="Section 8 Client Interaction"/>
            /// </remarks>
            if (await RequestHandler.TryGetCommandResult(Command, out ClientHandlingResult existingResult))
            {
                return existingResult;
            }

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
                result = Responsibilities.QueueBlockingEventAction<ClientHandlingResult>(
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

            return result;
        }
    }
}
