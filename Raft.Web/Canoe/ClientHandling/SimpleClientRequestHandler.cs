using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using Raft.Web.Canoe.ClientHandling.Notes;
using Raft.Web.Canoe.Logging;

namespace Raft.Web.Canoe.ClientHandling
{
    public class SimpleClientRequestHandler : IClientRequestHandler
    {
        public SimpleClientRequestHandler(INotes notes, IEventLogger logger)
        {
            Notes = notes;
            Logger = logger;
        }

        public INotes Notes { get; }
        public IEventLogger Logger { get; }

        bool IncludeCommand => ComponentContainer.Instance.GetInstance<IOtherConfiguration>().IncludeOriginalClientCommandInResults;


        public Task ExecuteAndApplyLogEntry(ICommand logEntryCommand)
        {
            Logger.LogInformation(nameof(SimpleClientRequestHandler), logEntryCommand.UniqueId, $"Received command to process by State Machine: {logEntryCommand?.Stringify()}");

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
            Logger.LogInformation(nameof(SimpleClientRequestHandler), command.UniqueId, $"Received command {nameof(TryGetCommandResult)}: {command?.Stringify()}");

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
