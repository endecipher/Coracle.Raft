using ActivityLogger.Logging;
using ActivityLogLevel = ActivityLogger.Logging.ActivityLogLevel;
using Coracle.IntegrationTests.Components.Logging;
using Newtonsoft.Json;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Logs.CollectionStructure;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Logs;

namespace Coracle.IntegrationTests.Components.PersistentData
{
    /// <summary>
    /// For testing purposes, having an in-memory implementation
    /// </summary>
    public class TestLogHolder : IPersistentReplicatedLogHolder
    {
        #region Constants 
        public const string Entity = nameof(TestLogHolder);
        public const string LogChain = nameof(LogChain);
        public const string OverwritingEntries = nameof(OverwritingEntries);
        public const string AppendNew = nameof(AppendNew);
        public const string AppendNoOp = nameof(AppendNoOp);
        public const string AppendConf = nameof(AppendConf);
        public const string DeleteAllEntriesStartingFrom = nameof(DeleteAllEntriesStartingFrom);

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
                    Contents = null,
                    Type = LogEntry.Types.None
                }
            };
            Properties = persistentProperties;
        }

        public IPersistentProperties Properties { get; }

        private void Snap(string eventString)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = eventString,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(LogChain, LogEntries.Select(x => JsonConvert.SerializeObject(x)).Aggregate((x, y) => $"{x} -> {y}")))
            .WithCallerInfo());
        }

        public Task OverwriteEntries(IEnumerable<LogEntry> logEntries)
        {
            lock (obj)
            {
                var index = logEntries.First().CurrentIndex;

                DeleteAllEntriesStartingFromIndex(index);

                LogEntries.InsertRange((int)index, logEntries);

                Snap(OverwritingEntries);

                return Task.CompletedTask;
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
                return Task.FromResult(LogEntries.GetRange((int)startIndex, (int)endIndex - (int)startIndex + 1) as IEnumerable<LogEntry>);
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
                try
                {
                    return Task.FromResult(LogEntries[(int)index]);
                }
                catch //In case the NextIndex is 0 (from OnSendAppendEntriesRPC), the Previous Index will be -1, which doesn't exist. Thus handling that case
                {
                    return Task.FromResult<LogEntry>(null);
                }
            }
        }

        public Task<LogEntry> TryGetValueAtLastIndex()
        {
            lock (obj)
            {
                return Task.FromResult(LogEntries.Last());
            }
        }


        public async Task<LogEntry> AppendNewCommandEntry<TCommand>(TCommand inputCommand) where TCommand : class, ICommand
        {
            long term = await Properties.GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    CurrentIndex = LogEntries.Count,
                    Term = term,
                    Contents = inputCommand,
                    Type = LogEntry.Types.Command
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
                    CurrentIndex = LogEntries.Count,
                    Term = term,
                    Contents = null,
                    Type = LogEntry.Types.NoOperation

                };

                LogEntries.Add(item);

                Snap(AppendNoOp);

                return;
            }
        }

        public async Task<LogEntry> AppendConfigurationEntry(IEnumerable<NodeConfiguration> configurations)
        {
            long term = await Properties.GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    CurrentIndex = LogEntries.Count,
                    Term = term,
                    Contents = configurations,
                    Type = LogEntry.Types.Configuration
                };

                LogEntries.Add(item);

                Snap(AppendConf);

                return item;
            }
        }

        public Task<IEnumerable<NodeConfiguration>> ReadFrom(LogEntry configurationLogEntry)
        {
            if (configurationLogEntry.Contents is IEnumerable<NodeConfiguration> conf)
                return Task.FromResult(conf);
            
            throw new InvalidOperationException();
        }

        public Task<ICommand> ReadFrom<ICommand>(LogEntry commandLogEntry)
        {
            if (commandLogEntry.Contents is ICommand command)
                return Task.FromResult(command);

            throw new InvalidOperationException();
        }
    }
}
