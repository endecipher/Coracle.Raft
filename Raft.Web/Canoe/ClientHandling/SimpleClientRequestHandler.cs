#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

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
