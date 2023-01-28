using ActivityLogger.Logging;
using ActivityLogLevel = ActivityLogger.Logging.ActivityLogLevel;
using Newtonsoft.Json;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Logs.CollectionStructure;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Logs;
using Coracle.Samples.Logging;

namespace Coracle.IntegrationTests.Components.PersistentData
{
    /// <summary>
    /// For testing purposes, having an in-memory implementation
    /// </summary>
    public class TestLogHolder : IPersistentReplicatedLogHolder
    {
        #region Constants 
        public const string Entity = nameof(TestLogHolder);
        public const string logChain = nameof(logChain);
        public const string OverwritingEntries = nameof(OverwritingEntries);
        public const string OverwrittenEntries = nameof(OverwrittenEntries);
        public const string AppendedCommand = nameof(AppendedCommand);
        public const string AppendNoOp = nameof(AppendNoOp);
        public const string AppendedConf = nameof(AppendedConf);
        public const string DeleteAllEntriesStartingFrom = nameof(DeleteAllEntriesStartingFrom);

        #endregion 
        IActivityLogger ActivityLogger { get; }

        private object obj = new object();
        public Element RootElement { get; private set; }
        public Element LastElement { get; private set; }

        public TestLogHolder(IPersistentProperties persistentProperties, IActivityLogger activityLogger)
        {
            RootElement = LastElement = new Element
            {
                Entry = new LogEntry
                {
                    CurrentIndex = 0,
                    Term = 0,
                    Content = null,
                    Type = LogEntry.Types.None
                },
                NextElement = null,
                PreviousElement = null,
            };

            Properties = persistentProperties;

            ActivityLogger = activityLogger;
        }

        public IPersistentProperties Properties { get; }

        public class SampleDisplayEntry
        {
            public object _ { get; init; }
            public long Term { get; init; }
            public long Index { get; set; }
            public string Type { get; init; }
        }

        private void Snap(string eventString)
        {
            List<SampleDisplayEntry> GetLogChainEntries()
            {
                var list = new List<SampleDisplayEntry>();

                var currentElement = RootElement;

                while (currentElement != null)
                {
                    list.Add(new SampleDisplayEntry
                    {
                        _ = currentElement.Entry.Content,
                        Index = currentElement.Entry.CurrentIndex,
                        Term = currentElement.Entry.Term,
                        Type = currentElement.Entry.Type.ToString()
                    });

                    currentElement = currentElement.NextElement;
                }

                return list;
            }

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = eventString,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(logChain, GetLogChainEntries()))
            .WithCallerInfo());
        }

        public Task OverwriteEntries(IEnumerable<LogEntry> logEntries)
        {
            lock (obj)
            {
                Snap(OverwritingEntries);

                var firstIndexToOverwrite = logEntries.First().CurrentIndex;

                var currentElement = RootElement;

                bool isFound = false;

                //Obtain the element which has the firstIndexToOverWriteFrom
                while (currentElement != null)
                {
                    if (currentElement.Entry.CurrentIndex.Equals(firstIndexToOverwrite))
                    {
                        isFound = true;
                        break;
                    }

                    currentElement = currentElement.NextElement;
                }

                int skipCount = 0;

                if (isFound)
                {
                    //If found element is the first element itself, we would need to append all after it
                    if (currentElement.Entry.Type.HasFlag(LogEntry.Types.None))
                    {
                        skipCount = 1;
                        LastElement = currentElement;
                    }
                    else
                    {
                        //Since we want to delete the chain from [firstIndexToOverWriteFrom, LastElement.Entry.CurrentIndex],
                        //we make the LastElement as the previousElement of the one found above
                        LastElement = currentElement.PreviousElement;
                    }
                }
                

                //Update Link
                LastElement.NextElement = null;

                //Append from the LastElement, all the new entries afresh
                foreach (var entryToAppend in logEntries.Skip(skipCount))
                {
                    var newElement = new Element
                    {
                        Entry = entryToAppend,
                        NextElement = null,
                        PreviousElement = LastElement,
                    };

                    // Link LastNode
                    LastElement.NextElement = newElement;

                    //Make new element as last node
                    LastElement = newElement;
                }

                Snap(OverwrittenEntries);

                return Task.CompletedTask;
            }
        }

        public Task<bool> DoesTermExist(long termNumber)
        {
            lock (obj)
            {
                var currentElement = RootElement;

                while (currentElement != null)
                {
                    if (currentElement.Entry.Term.Equals(termNumber))
                        return Task.FromResult(true);

                    currentElement = currentElement.NextElement;
                }

                return Task.FromResult(false);
            }
        }

        public Task<IEnumerable<LogEntry>> FetchLogEntriesBetween(long startIndex, long endIndex)
        {
            lock (obj)
            {
                var currentElement = RootElement;

                var sectionList = new List<LogEntry>();

                while (currentElement != null && currentElement.Entry.CurrentIndex <= endIndex)
                {
                    if (startIndex <= currentElement.Entry.CurrentIndex && currentElement.Entry.CurrentIndex <= endIndex)
                        sectionList.Add(currentElement.Entry);

                    currentElement = currentElement.NextElement;
                }

                return Task.FromResult(sectionList as IEnumerable<LogEntry>);
            }
        }

        public Task<long?> GetFirstIndexForTerm(long termNumber)
        {
            lock (obj)
            {
                var currentElement = RootElement;
                long? firstIndexFound = null;

                while (currentElement != null)
                {
                    if (currentElement.Entry.Term.Equals(termNumber))
                    {
                        firstIndexFound = currentElement.Entry.CurrentIndex;
                        break;
                    }

                    currentElement = currentElement.NextElement;
                }

                return Task.FromResult(firstIndexFound);
            }
        }

        public Task<long> GetLastIndex()
        {
            lock (obj)
            {
                return Task.FromResult(LastElement.Entry.CurrentIndex);
            }
        }

        public Task<long> GetLastIndexForTerm(long termNumber)
        {
            lock (obj)
            {
                var currentElement = LastElement;

                while (currentElement != null)
                {
                    if (currentElement.Entry.Term.Equals(termNumber))
                    {
                        return Task.FromResult(currentElement.Entry.CurrentIndex); 
                    }

                    currentElement = currentElement.PreviousElement;
                }

                throw new InvalidOperationException("Term doesn't exist in Linked List");
            }
        }

        public Task<long> GetTermAtIndex(long index)
        {
            lock (obj)
            {
                var currentElement = RootElement;

                while (currentElement != null)
                {
                    if (currentElement.Entry.CurrentIndex.Equals(index))
                    {
                        return Task.FromResult(currentElement.Entry.Term);
                    }

                    currentElement = currentElement.NextElement;
                }

                throw new InvalidOperationException("Index doesn't exist in Linked List");
            }
        }

        public Task<LogEntry> TryGetValueAtIndex(long index)
        {
            lock (obj)
            {
                var currentElement = RootElement;

                while (currentElement != null)
                {
                    if (currentElement.Entry.CurrentIndex.Equals(index))
                    {
                        return Task.FromResult(currentElement.Entry);
                    }

                    currentElement = currentElement.NextElement;
                }

                return Task.FromResult<LogEntry>(null);
            }
        }

        public Task<LogEntry> TryGetValueAtLastIndex()
        {
            lock (obj)
            {
                return Task.FromResult(LastElement.Entry);
            }
        }


        public async Task<LogEntry> AppendNewCommandEntry<TCommand>(TCommand inputCommand) where TCommand : class, ICommand
        {
            long term = await Properties.GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    Term = term,
                    Content = inputCommand,
                    Type = LogEntry.Types.Command,
                };

                Append(item);

                Snap(AppendedCommand);

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
                    Term = term,
                    Content = null,
                    Type = LogEntry.Types.NoOperation,
                };

                Append(item);

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
                    Term = term,
                    Content = configurations,
                    Type = LogEntry.Types.Configuration,
                };

                Append(item);

                Snap(AppendedConf);

                return item;
            }
        }

        public Task<IEnumerable<NodeConfiguration>> ReadFrom(LogEntry configurationLogEntry)
        {
            if (configurationLogEntry.Content is IEnumerable<NodeConfiguration> conf)
                return Task.FromResult(conf);
            
            throw new InvalidOperationException();
        }

        public Task<ICommand> ReadFrom<ICommand>(LogEntry commandLogEntry)
        {
            if (commandLogEntry.Content is ICommand command)
                return Task.FromResult(command);

            throw new InvalidOperationException();
        }

        private void Append(LogEntry entry)
        {
            var indexToStamp = LastElement.Entry.CurrentIndex + 1;

            entry.CurrentIndex = indexToStamp;

            var newElement = new Element
            {
                Entry = entry,
                NextElement = null,
                PreviousElement = LastElement,
            };

            // Link LastNode
            LastElement.NextElement = newElement;

            //Make new element as last node
            LastElement = newElement;
        }

        public async Task<long> FindValidTermPreviousTo(long invalidTerm)
        {
            const int initializationTerm = 0; // First entry's Term i.e Type = None

            var termToCheck = invalidTerm - 1; // Trying to see if Term exists by decrementing the invalid term

            while (invalidTerm > initializationTerm)
            {
                if (await DoesTermExist(termToCheck))
                {
                    return termToCheck;
                }
                else
                {
                    invalidTerm--; // Decrement again, as term doesn't exist
                }
            }

            return initializationTerm;
        }

        public IEnumerable<LogEntry> GetAll()
        {
            var currentElement = RootElement;

            while (currentElement != null)
            {
                yield return currentElement.Entry;

                currentElement = currentElement.NextElement;
            }
        }
    }

    public class Element
    {
        public LogEntry Entry { get; set; }

        public Element NextElement { get; set; }

        public Element PreviousElement { get; set; }
    }

    public class ElementList
    {

    }
}
