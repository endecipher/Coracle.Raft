using ActivityLogger.Logging;
using Coracle.Raft.Engine.Command;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Snapshots;
using Coracle.Samples.Data;
using Coracle.Samples.Logging;

namespace Coracle.Samples.ClientHandling
{
    public class NoteKeeperStateMachineHandler : IStateMachineHandler
    {
        #region Constants
        private const string SimpleClientRequestHandlerEntity = nameof(NoteKeeperStateMachineHandler);
        private const string ExecutingAndApplyingLogEntryCommand = nameof(ExecutingAndApplyingLogEntryCommand);
        private const string GetCommandResult = nameof(GetCommandResult);
        private const string ForceRebuild = nameof(ForceRebuild);
        private const string UniqueCommandId = nameof(UniqueCommandId);
        private const string LogEntryCommand = nameof(LogEntryCommand);
        private const string snapshotDetails = nameof(snapshotDetails);
        #endregion

        public NoteKeeperStateMachineHandler(INoteStorage notes, IActivityLogger activityLogger, IEngineConfiguration engineConfiguration, ISnapshotManager snapshotManager)
        {
            Notes = notes;
            ActivityLogger = activityLogger;
            EngineConfiguration = engineConfiguration;
            SnapshotManager = snapshotManager;
        }

        INoteStorage Notes { get; }
        IEngineConfiguration EngineConfiguration { get; }
        public ISnapshotManager SnapshotManager { get; }
        IActivityLogger ActivityLogger { get; }
        bool IncludeCommand => EngineConfiguration.IncludeOriginalClientCommandInResults;

        LatestCommandDetails LatestCommand { get; set; }

        class LatestCommandDetails
        {
            public string CommandId { get; set; }
            public CommandExecutionResult Result { get; internal set; }
        }

        public async Task ExecuteAndApply(ICommand logEntryCommand)
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

            if (logEntryCommand == null || logEntryCommand.IsReadOnly || !logEntryCommand.Type.Equals(NoteStorage.AddNote))
            {
                return;
            }

            if (logEntryCommand is NoteCommand baseNoteCommand)
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
                Result = new CommandExecutionResult
                {
                    IsSuccessful = true,
                    OriginalCommand = IncludeCommand ? logEntryCommand : null,
                }
            };

            await Task.CompletedTask;

            return;
        }

        public Task<(bool IsExecutedAndLatest, CommandExecutionResult CommandResult)> IsExecutedAndLatest(string uniqueCommandId)
        {
            if (LatestCommand != null && LatestCommand.CommandId.Equals(uniqueCommandId))
            {
                return Task.FromResult((IsExecutedAndLatest: true, CommandResult: LatestCommand.Result));
            }

            return Task.FromResult((IsExecutedAndLatest: false, CommandResult: default(CommandExecutionResult)));
        }

        public Task<(bool IsExecuted, CommandExecutionResult CommandResult)> TryGetResult<TCommand>(TCommand command)
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

            var result = new CommandExecutionResult();

            switch (command.Type)
            {
                case NoteStorage.AddNote:
                    {
                        bool hasNote = Notes.HasNote((command as NoteCommand).Data);
                        result.IsSuccessful = true;
                        result.OriginalCommand = IncludeCommand ? command : null;

                        /// Since there exists a command.Data/Note, we can be sure that the Command was applied
                        return Task.FromResult((IsExecuted: hasNote, CommandResult: result));
                    }
                case NoteStorage.GetNote:
                    {
                        /// For Readonly requests, we don't have to check whether command was applied
                        bool hasNote = Notes.TryGet((command as NoteCommand).Data.UniqueHeader, out var note);
                        result.IsSuccessful = true;
                        result.OriginalCommand = IncludeCommand ? command : null;
                        result.CommandResult = note;
                        return Task.FromResult((IsExecuted: true, CommandResult: result));
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
