using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.ClientHandling.Notes;
using Coracle.IntegrationTests.Components.Logging;

namespace Coracle.IntegrationTests.Components.ClientHandling
{
    public class TestClientRequestHandler : IClientRequestHandler
    {
        #region Constants
        private const string SimpleClientRequestHandlerEntity = nameof(TestClientRequestHandler);
        private const string ExecutingAndApplyingLogEntryCommand = nameof(ExecutingAndApplyingLogEntryCommand);
        private const string GetCommandResult = nameof(GetCommandResult);
        private const string UniqueCommandId = nameof(UniqueCommandId);
        private const string LogEntryCommand = nameof(LogEntryCommand);
        #endregion

        public TestClientRequestHandler(INotes notes, IActivityLogger activityLogger, IEngineConfiguration engineConfiguration)
        {
            Notes = notes;
            ActivityLogger = activityLogger;
            EngineConfiguration = engineConfiguration;
        }

        INotes Notes { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IActivityLogger ActivityLogger { get; }
        bool IncludeCommand => EngineConfiguration.IncludeOriginalClientCommandInResults;

        LatestCommandDetails LatestCommand { get; set; }

        class LatestCommandDetails
        {
            public string CommandId { get; set; }
            public ClientHandlingResult Result { get; internal set; }
        }

        public async Task ExecuteAndApplyLogEntry(ICommand logEntryCommand)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = SimpleClientRequestHandlerEntity,
                Event = ExecutingAndApplyingLogEntryCommand,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(UniqueCommandId, logEntryCommand.UniqueId))
            .With(ActivityParam.New(LogEntryCommand, logEntryCommand))
            .WithCallerInfo());

            if (logEntryCommand == null || logEntryCommand.IsReadOnly || !logEntryCommand.Type.Equals(nameof(Notes.Add)))
            {
                return;
            }

            Notes.Add(logEntryCommand.Data as Note);

            LatestCommand = new LatestCommandDetails
            {
                CommandId = logEntryCommand.UniqueId,
                Result = new ClientHandlingResult
                {
                    IsOperationSuccessful = true,
                    OriginalCommand = IncludeCommand ? logEntryCommand : null,
                }
            };

            await Task.CompletedTask;

            return;
        }

        public Task<bool> IsCommandLatest(string uniqueCommandId, out ClientHandlingResult executedResult)
        {
            executedResult = null;

            if (LatestCommand != null && LatestCommand.CommandId.Equals(uniqueCommandId))
            {
                executedResult = LatestCommand.Result;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> TryGetCommandResult<TCommand>(TCommand command, out ClientHandlingResult result)
            where TCommand : class, ICommand
        {
            ActivityLogger?.Log(new ImplActivity
            {
                Description = $"Received command {nameof(TryGetCommandResult)}",
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
