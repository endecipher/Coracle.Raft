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

using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Logs;
using Core.Raft.Canoe.Engine.Logs.CollectionStructure;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Logging;
using Raft.Web.Canoe.Logging;

namespace Raft.Web.Canoe.PersistentData
{
    public class MemoryLogHolder : IPersistentReplicatedLogHolder
    {
        public List<LogEntry> LogEntries { get; private set; }
        public IEventLogger EventLogger { get; }
        public IPersistentProperties Properties { get; }

        private object obj = new object();

        public MemoryLogHolder(IEventLogger eventLogger, IPersistentProperties properties)
        {
            LogEntries = new List<LogEntry>()
            {
                new LogEntry
                {
                    CurrentIndex = 0,
                    Term = 0,
                    Command = null
                }
            };
            EventLogger = eventLogger;
            Properties = properties;
        }

        private int LastIndex => LogEntries.Count - 1;

        private string LogChain => LogEntries.Select(x => x.ToString()).Aggregate((x, y) => $"{x} -> {y}");

        private void Snap()
        {
            EventLogger.LogInformation(nameof(MemoryLogHolder), _message: $"{LogChain}");
        }

        public Task AppendEntriesWithOverwrite(IEnumerable<LogEntry> logEntries)
        {
            lock (obj)
            {
                var index = logEntries.First().CurrentIndex;

                DeleteAllEntriesStartingFromIndex(index);

                LogEntries.InsertRange((int)index, logEntries);

                Snap();

                return Task.CompletedTask;
            }
        }

        public async Task<LogEntry> AppendNewEntry<TCommand>(TCommand inputCommand) where TCommand : class, ICommand
        {
            long term = await Properties.GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    CurrentIndex = LogEntries.Count() - 1,
                    Term = term,
                    Command = inputCommand
                };

                LogEntries.Add(item);

                Snap();

                return item;
            }
        }

        public async Task AppendNoOperationEntry()
        {
            long term = await Properties.GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    CurrentIndex = LogEntries.Count() - 1,
                    Term = term,
                    Command = null
                };

                LogEntries.Add(item);

                Snap();

                return;
            }
        }

        public Task DeleteAllEntriesStartingFromIndex(long index)
        {
            int ix = (int)index;

            lock (obj)
            {
                //TODO: Check behavior. Assuming, it doesn't delete the [index, .. but does [index + 1, ..]. That's why added - 1
                LogEntries.RemoveRange(ix - 1, LogEntries.Count - ix + 1);

                Snap();

                return Task.CompletedTask;
            }
        }

        public Task<bool> DoesLogExistFor(long index, long term)
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.Where(x => x.Term == term && x.CurrentIndex == index).Any());
            }
        }

        public Task<bool> DoesTermExist(long termNumber)
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.Where(x => x.Term == termNumber).Any());
            }
        }

        public Task<IEnumerable<LogEntry>> FetchLogEntriesBetween(long startIndex, long endIndex)
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.GetRange((int)startIndex, (int)endIndex) as IEnumerable<LogEntry>);
            }
        }

        public Task<long?> GetFirstIndexForTerm(long termNumber)
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.FirstOrDefault(x => x.Term == termNumber)?.CurrentIndex);
            }
        }

        public Task<long> GetLastIndex()
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.Last().CurrentIndex);
            }
        }

        public Task<long> GetLastIndexForTerm(long termNumber)
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.Last(x => x.Term == termNumber).CurrentIndex);
            }
        }

        public Task<long> GetTermAtIndex(long index)
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries[(int)index].Term);
            }
        }

        public Task<LogEntry> TryGetValueAtIndex(long index)
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries[(int)index]);
            }
        }

        public Task<LogEntry> TryGetValueAtLastIndex()
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.Last());
            }
        }
    }

    public class MemoryProperties : IPersistentProperties
    {
        public long CurrentTerm { get; private set; }

        public string VotedFor { get; private set; }

        public MemoryProperties(IEventLogger logger)
        {
            Logger = logger;

            CurrentTerm = 0;
        }

        public IEventLogger Logger { get; }

        private void Snap()
        {
            Logger.LogInformation(nameof(MemoryProperties), _message: $"CurrentTerm: {CurrentTerm} | VotedFor: {VotedFor}");
        }

        public Task ClearVotedFor()
        {
            VotedFor = null;
            Snap();
            return Task.CompletedTask;
        }

        public Task<long> GetCurrentTerm()
        {
            Snap();
            return Task.FromResult(CurrentTerm);
        }

        public Task<string> GetVotedFor()
        {
            Snap();
            return Task.FromResult(VotedFor);
        }

        public Task<long> IncrementCurrentTerm()
        {
            CurrentTerm++;
            Snap();
            return Task.FromResult(CurrentTerm);
        }

        public Task SetCurrentTerm(long termNumber)
        {
            CurrentTerm = termNumber;
            Snap();
            return Task.CompletedTask;
        }

        public Task SetVotedFor(string serverUniqueId)
        {
            VotedFor = serverUniqueId;
            Snap();
            return Task.CompletedTask;
        }
    }
}
