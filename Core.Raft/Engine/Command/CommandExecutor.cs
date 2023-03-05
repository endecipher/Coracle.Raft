using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Actions;

namespace Coracle.Raft.Engine.Command
{
    internal sealed class CommandExecutor : ICommandExecutor
    {
        public CommandExecutor(
            IStateMachineHandler clientRequestHandler,
            IEngineConfiguration engineConfiguration,
            ILeaderNodePronouncer leaderNodePronouncer,
            ICurrentStateAccessor currentStateAccessor,
            IResponsibilities responsibilities,
            IGlobalAwaiter globalAwaiter,
            IPersistentStateHandler persistentState,
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

        IStateMachineHandler RequestHandler { get; }
        IEngineConfiguration EngineConfiguration { get; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        IPersistentStateHandler PersistentState { get; }
        IActivityLogger ActivityLogger { get; }
        IAppendEntriesManager AppendEntriesManager { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IResponsibilities Responsibilities { get; }

        public Task<CommandExecutionResult> Execute<TCommand>(TCommand Command, CancellationToken cancellationToken) where TCommand : class, ICommand
        {
            CommandExecutionResult result;

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
                result = Responsibilities.QueueBlockingAction<CommandExecutionResult>(
                            action: action,
                            executeSeparately: false
                        );
            }
            catch (Exception e)
            {
                result = new CommandExecutionResult
                {
                    IsSuccessful = false,
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
