using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;
using EventGuidance.Dependency;
using Coracle.Web.ClientHandling.Notes;
using ActivityLogger.Logging;
using Coracle.Web.Coracle.Logging;

namespace Coracle.Web.ClientHandling
{
    public class SimpleClientRequestHandler : IClientRequestHandler
    {
        #region Constants
        private const string SimpleClientRequestHandlerEntity = nameof(SimpleClientRequestHandler);
        private const string ExecutingAndApplyingLogEntryCommand = nameof(ExecutingAndApplyingLogEntryCommand);
        private const string GetCommandResult = nameof(GetCommandResult);
        private const string UniqueCommandId = nameof(UniqueCommandId);
        private const string LogEntryCommand = nameof(LogEntryCommand);
        #endregion

        public SimpleClientRequestHandler(INotes notes)
        {
            Notes = notes;
        }

        public INotes Notes { get; }
        IActivityLogger ActivityLogger => ComponentContainer.Instance.GetInstance<IActivityLogger>();

        bool IncludeCommand => ComponentContainer.Instance.GetInstance<IEngineConfiguration>().IncludeOriginalClientCommandInResults;


        public Task ExecuteAndApplyLogEntry(ICommand logEntryCommand)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                Description = $"Received command to process by State Machine: {logEntryCommand?.Stringify()}",
                EntitySubject = SimpleClientRequestHandlerEntity,
                Event = ExecutingAndApplyingLogEntryCommand,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(UniqueCommandId, logEntryCommand.UniqueId))
            .With(ActivityParam.New(LogEntryCommand, logEntryCommand))
            .WithCallerInfo());

            if (logEntryCommand == null || logEntryCommand.IsReadOnly || !logEntryCommand.Type.Equals(nameof(Notes.Add)))
            {
                return Task.CompletedTask;
            }

            Notes.Add(logEntryCommand.Data as Note);

            return Task.CompletedTask;
        }

        public Task<bool> TryGetCommandResult<TCommand>(TCommand command, out ClientHandlingResult result)
            where TCommand : class, ICommand
        {
            ActivityLogger?.Log(new ImplActivity
            {
                Description = $"Received command {nameof(TryGetCommandResult)}: {command?.Stringify()}",
                EntitySubject = SimpleClientRequestHandlerEntity,
                Event = GetCommandResult,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(UniqueCommandId, command.UniqueId))
            .With(ActivityParam.New(LogEntryCommand, command))
            .WithCallerInfo());

            result = new ClientHandlingResult();

            switch (command.Type)
            {
                case nameof(INotes.Add):
                {
                    bool hasNote = Notes.HasNote(command.Data as Note);
                    result.IsOperationSuccessful = true;
                    result.OriginalCommand = IncludeCommand ? command : null;

                    /// Since there exists a command.Data/Note, we can be sure that the Command was applied
                    return Task.FromResult(hasNote);
                }
                case nameof(INotes.TryGet):
                {
                    /// For Readonly requests, we don't have to check whether command was applied
                    bool hasNote = Notes.TryGet(command.Data as string, out var note);
                    result.IsOperationSuccessful = true;
                    result.OriginalCommand = IncludeCommand ? command : null;
                    result.CommandResult = note;
                    return Task.FromResult(true);
                }
                default:
                    throw new ArgumentException($"Invalid Command Type");
            }
        }
    }
}
