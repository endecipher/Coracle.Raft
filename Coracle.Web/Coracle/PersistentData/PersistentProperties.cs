using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Logs;
using Core.Raft.Canoe.Engine.Logs.CollectionStructure;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Dependency;
using ActivityLogger.Logging;
using Coracle.Web.Coracle.Logging;
using ActivityLogLevel = ActivityLogger.Logging.ActivityLogLevel;

namespace Coracle.Web.PersistentData
{
    public class MemoryLogHolder : IPersistentReplicatedLogHolder
    {
        #region Constants 
        private const string ReplicatedLogHolderEntity = nameof(MemoryLogHolder);
        private const string LogChain = nameof(LogChain);
        private const string OverwriteEntries = nameof(OverwriteEntries);
        private const string AppendNew = nameof(AppendNew);
        private const string AppendNoOp = nameof(AppendNoOp);
        private const string DeleteAllEntriesStartingFrom = nameof(DeleteAllEntriesStartingFrom);

        #endregion 
        IActivityLogger ActivityLogger => ComponentContainer.Instance.GetInstance<IActivityLogger>();
        public List<LogEntry> LogEntries { get; private set; }
        public IPersistentProperties Properties { get; }

        private object obj = new object();

        public MemoryLogHolder(IPersistentProperties properties)
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
            Properties = properties;
        }

        private int LastIndex => LogEntries.Count - 1;

        private void Snap(string eventString)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = ReplicatedLogHolderEntity,
                Event = eventString,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(LogChain, LogEntries.Select(x => x.ToString()).Aggregate((x, y) => $"{x} -> {y}")))
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
                    Command = inputCommand
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
                    CurrentIndex = LogEntries.Count() - 1,
                    Term = term,
                    Command = null
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

    public class MemoryProperties : IPersistentProperties
    {
        #region Constants 
        private const string PersistentPropertiesEntity = nameof(MemoryProperties);
        private const string CurrentTermValue = nameof(CurrentTermValue);
        private const string VotedForValue = nameof(VotedForValue);
        private const string GetValue = nameof(GetValue);
        private const string ClearingVotedFor = nameof(ClearingVotedFor);
        private const string IncrementedCurrentTerm = nameof(IncrementedCurrentTerm);
        private const string SetCurrentTermExternally = nameof(SetCurrentTermExternally);
        private const string SetVotedForExternally = nameof(SetVotedForExternally);
        #endregion

        IActivityLogger ActivityLogger => ComponentContainer.Instance.GetInstance<IActivityLogger>();

        public long CurrentTerm { get; private set; }

        public string VotedFor { get; private set; }

        public MemoryProperties()
        {
            CurrentTerm = 0;
        }

        private void Snap(string eventString = GetValue)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = PersistentPropertiesEntity,
                Event = eventString,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(CurrentTermValue, CurrentTerm))
            .With(ActivityParam.New(VotedForValue, CurrentTerm))
            .WithCallerInfo());
        }

        public Task ClearVotedFor()
        {
            VotedFor = null;
            Snap(ClearingVotedFor);
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
            Snap(IncrementedCurrentTerm);
            return Task.FromResult(CurrentTerm);
        }

        public Task SetCurrentTerm(long termNumber)
        {
            CurrentTerm = termNumber;
            Snap(SetCurrentTermExternally);
            return Task.CompletedTask;
        }

        public Task SetVotedFor(string serverUniqueId)
        {
            VotedFor = serverUniqueId;
            Snap(SetVotedForExternally);
            return Task.CompletedTask;
        }
    }
}
