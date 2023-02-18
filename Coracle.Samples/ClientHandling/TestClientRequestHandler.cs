using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Samples.ClientHandling.NoteCommand;
using Coracle.Samples.ClientHandling.Notes;
using Coracle.Samples.Logging;
using Coracle.Samples.PersistentData;

namespace Coracle.Samples.ClientHandling
{
    public class TestClientRequestHandler : IClientRequestHandler
    {
        #region Constants
        private const string SimpleClientRequestHandlerEntity = nameof(TestClientRequestHandler);
        private const string ExecutingAndApplyingLogEntryCommand = nameof(ExecutingAndApplyingLogEntryCommand);
        private const string GetCommandResult = nameof(GetCommandResult);
        private const string ForceRebuild = nameof(ForceRebuild);
        private const string UniqueCommandId = nameof(UniqueCommandId);
        private const string LogEntryCommand = nameof(LogEntryCommand);
        private const string snapshotDetails = nameof(snapshotDetails);
        #endregion

        public TestClientRequestHandler(INotes notes, IActivityLogger activityLogger, IEngineConfiguration engineConfiguration, ISnapshotManager snapshotManager)
        {
            Notes = notes;
            ActivityLogger = activityLogger;
            EngineConfiguration = engineConfiguration;
            SnapshotManager = snapshotManager;
        }

        INotes Notes { get; }
        IEngineConfiguration EngineConfiguration { get; }
        public ISnapshotManager SnapshotManager { get; }
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

            if (logEntryCommand == null || logEntryCommand.IsReadOnly || !logEntryCommand.Type.Equals(ClientHandling.Notes.Notes.AddNote))
            {
                return;
            }

            if (logEntryCommand is NoteCommand.NoteCommand baseNoteCommand)
            {
                Notes.Add(baseNoteCommand.Data);
            }
            else
            {
                throw new InvalidOperationException("Invalid Command Type during conversion");
            }

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
                case ClientHandling.Notes.Notes.AddNote:
                    {
                        bool hasNote = Notes.HasNote((command as NoteCommand.NoteCommand).Data);
                        result.IsOperationSuccessful = true;
                        result.OriginalCommand = IncludeCommand ? command : null;

                        /// Since there exists a command.Data/Note, we can be sure that the Command was applied
                        return Task.FromResult(hasNote);
                    }
                case ClientHandling.Notes.Notes.GetNote:
                    {
                        /// For Readonly requests, we don't have to check whether command was applied
                        bool hasNote = Notes.TryGet((command as NoteCommand.NoteCommand).Data.UniqueHeader, out var note);
                        result.IsOperationSuccessful = true;
                        result.OriginalCommand = IncludeCommand ? command : null;
                        result.CommandResult = note;
                        return Task.FromResult(true);
                    }
                default:
                    throw new ArgumentException($"Invalid Command Type");
            }
        }

        public async Task ForceRebuildFromSnapshot(ISnapshotHeader snapshot)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = SimpleClientRequestHandlerEntity,
                Event = ForceRebuild,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(snapshotDetails, snapshot))
            .WithCallerInfo());

            var file = await SnapshotManager.GetFile(snapshot);

            var data = await file.ReadData();

            Notes.Build(data);
        }
    }
}
