using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Logs;
using Core.Raft.Canoe.Engine.Logs.CollectionStructure;
using Core.Raft.Canoe.Engine.States;
using ActivityLogger.Logging;
using ActivityLogLevel = ActivityLogger.Logging.ActivityLogLevel;
using Coracle.IntegrationTests.Components.Logging;
using Newtonsoft.Json;

namespace Coracle.IntegrationTests.Components.PersistentData
{
    /// <summary>
    /// For testing purposes, having an in-memory implementation
    /// </summary>
    public class TestLogHolder : IPersistentReplicatedLogHolder
    {
        #region Constants 
        private const string ReplicatedLogHolderEntity = nameof(TestLogHolder);
        private const string LogChain = nameof(LogChain);
        private const string OverwriteEntries = nameof(OverwriteEntries);
        private const string AppendNew = nameof(AppendNew);
        private const string AppendNoOp = nameof(AppendNoOp);
        private const string DeleteAllEntriesStartingFrom = nameof(DeleteAllEntriesStartingFrom);

        #endregion 
        IActivityLogger ActivityLogger { get; }
        public List<LogEntry> LogEntries { get; private set; }

        private object obj = new object();

        public TestLogHolder(IPersistentProperties persistentProperties)
        {
            LogEntries = new List<LogEntry>()
            {
                new LogEntry
                {
                    CurrentIndex = 0,
                    Term = 0,
                    Contents = null as ICommand,
                    IsConfiguration = false,
                    IsEmpty = true,
                    IsExecutable = false,
                }
            };
            Properties = persistentProperties;
        }

        private int LastIndex => LogEntries.Count - 1;

        public IPersistentProperties Properties { get; }

        private void Snap(string eventString)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = ReplicatedLogHolderEntity,
                Event = eventString,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(LogChain, LogEntries.Select(x => JsonConvert.SerializeObject(x)).Aggregate((x, y) => $"{x} -> {y}")))
            .WithCallerInfo());
        }

        public Task AppendEntriesWithOverwrite(IEnumerable<LogEntry> logEntries)
        {
            lock (obj)
            {
                var index = logEntries.First().CurrentIndex;

                DeleteAllEntriesStartingFromIndex(index);

                LogEntries.InsertRange((int)index, logEntries);

                Snap(OverwriteEntries);

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
                    Contents = inputCommand,
                    IsConfiguration = false,
                    IsExecutable = true,
                    IsEmpty = false
                };

                LogEntries.Add(item);

                Snap(AppendNew);

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
                    CurrentIndex = LogEntries.Count - 1,
                    Term = term,
                    Contents = null,
                    IsExecutable = false,
                    IsConfiguration = false,
                    IsEmpty = true,

                };

                LogEntries.Add(item);

                Snap(AppendNoOp);

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

                Snap(DeleteAllEntriesStartingFrom);

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
}
