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

using ActivityLogger.Logging;
using ActivityLogLevel = ActivityLogger.Logging.ActivityLogLevel;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Engine.Configuration.Cluster;
using Newtonsoft.Json;
using System.Text;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Snapshots;
using Coracle.Raft.Engine.Command;
using Coracle.Raft.Examples.Logging;

namespace Coracle.Raft.Examples.Data
{
    public class SampleVolatileStateHandler : IPersistentStateHandler
    {
        #region Constants 
        public const string Entity = nameof(SampleVolatileStateHandler);
        public const string CurrentTermValue = nameof(CurrentTermValue);
        public const string VotedForValue = nameof(VotedForValue);
        public const string GetValue = nameof(GetValue);
        public const string ClearingVotedFor = nameof(ClearingVotedFor);
        public const string IncrementedCurrentTerm = nameof(IncrementedCurrentTerm);
        public const string SetCurrentTermExternally = nameof(SetCurrentTermExternally);
        public const string SetVotedForExternally = nameof(SetVotedForExternally);
        #endregion

        #region Log Holder Constants 
        public const string EntityLog = nameof(EntityLog);
        public const string logChain = nameof(logChain);
        public const string OverwritingEntries = nameof(OverwritingEntries);
        public const string OverwrittenEntries = nameof(OverwrittenEntries);
        public const string MergedSnapshot = nameof(MergedSnapshot);
        public const string AppendedCommand = nameof(AppendedCommand);
        public const string AppendNoOp = nameof(AppendNoOp);
        public const string AppendedConf = nameof(AppendedConf);
        public const string DeleteAllEntriesStartingFrom = nameof(DeleteAllEntriesStartingFrom);
        #endregion 

        IActivityLogger ActivityLogger { get; }
        public ISnapshotManager SnapshotManager { get; }
        public IClusterConfiguration ClusterConfiguration { get; }
        public IEngineConfiguration EngineConfiguration { get; }
        public long CurrentTerm { get; private set; }
        public string VotedFor { get; private set; }

        public SampleVolatileStateHandler(IActivityLogger activityLogger, ISnapshotManager snapshotManager, IClusterConfiguration clusterConfiguration, IEngineConfiguration engineConfiguration)
        {
            CurrentTerm = 0;
            ActivityLogger = activityLogger;
            SnapshotManager = snapshotManager;
            ClusterConfiguration = clusterConfiguration;
            EngineConfiguration = engineConfiguration;
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
        }

        private void Snap(string eventString = GetValue)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = eventString,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(CurrentTermValue, CurrentTerm))
            .With(ActivityParam.New(VotedForValue, VotedFor))
            .WithCallerInfo());
        }

        #region Core

        public Task ClearVotedFor()
        {
            VotedFor = null;

            Snap(ClearingVotedFor);

            return Task.CompletedTask;
        }

        public Task<long> GetCurrentTerm()
        {
            return Task.FromResult(CurrentTerm);
        }

        public Task<string> GetVotedFor()
        {
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

        #endregion

        #region Snapshot


        public Task<ISnapshotHeader> GetCommittedSnapshot()
        {
            lock (obj)
            {
                var supposedSnapshotElement = RootElement.NextElement;

                var hasSnapshot = supposedSnapshotElement != null
                    && supposedSnapshotElement.Entry.Type.HasFlag(LogEntry.Types.Snapshot);

                if (hasSnapshot)
                {
                    return Task.FromResult(supposedSnapshotElement.Entry.Content as ISnapshotHeader);
                }

                return Task.FromResult<ISnapshotHeader>(null);
            }
        }

        public Task<(bool HasSnapshot, ISnapshotHeader SnapshotDetail)> HasCommittedSnapshot(long inclusiveIndex)
        {
            lock (obj)
            {
                var supposedSnapshotElement = RootElement.NextElement;

                var isIndexZero = inclusiveIndex == default;

                /// <remarks>
                /// Say we have 0(Root)->Snap[1-50]->51->52->53
                /// If inclusiveIndex = 37, it would belong in a snapshot, and we must return back true.
                /// </remarks>

                var hasSnapshot = supposedSnapshotElement != null
                    && supposedSnapshotElement.Entry.Type.HasFlag(LogEntry.Types.Snapshot);

                var isIndexInsideSnapshotEntry = supposedSnapshotElement.Entry.CurrentIndex >= inclusiveIndex;

                bool hasSnapshotToSend = hasSnapshot && (isIndexZero || isIndexInsideSnapshotEntry);
                var snapshotToSend = RootElement.NextElement.Entry.Content as ISnapshotHeader;

                return Task.FromResult((hasSnapshotToSend, snapshotToSend));
            }
        }

        public async Task MarkInstallation(ISnapshotHeader snapshot)
        {
            if (await IsCommittedSnapshot(snapshot))
            {
                await SnapshotManager.UpdateLastUsed(snapshot);
                return;
            }

            throw new InvalidOperationException("Cannot mark installation for an uncommitted snapshot");
        }

        public async Task MarkCompletion(ISnapshotHeader snapshot)
        {
            if (await IsCommittedSnapshot(snapshot))
            {
                await SnapshotManager.UpdateLastUsed(snapshot);
                return;
            }

            throw new InvalidOperationException("Cannot mark completion for an uncommitted snapshot");
        }

        public async Task<bool> IsEligibleForCompaction(long commitIndex, long lastApplied, TimeSpan waitPeriodMs, int snapshotThresholdSize, int snapshotBufferSizeFromLastEntry)
        {
            var currentCommittedSnapshot = await GetCurrentCommittedSnapshotFromLogs();

            if (currentCommittedSnapshot != null)
            {
                bool IsWithinWaitPeriod(DateTimeOffset lastUsedTime, TimeSpan waitPeriod)
                {
                    var before = DateTimeOffset.Now.Add(waitPeriod * -1);

                    return lastUsedTime >= before;
                }

                var lastUsed = await SnapshotManager.GetLastUsed(currentCommittedSnapshot);

                if (IsWithinWaitPeriod(lastUsed, waitPeriodMs))
                {
                    return false;
                }
            }

            lock (obj)
            {
                int compactableNodeCount = 0;

                var currentElement = RootElement.NextElement;

                while (currentElement != null && currentElement.Entry.CurrentIndex <= lastApplied)
                {
                    ++compactableNodeCount;

                    currentElement = currentElement.NextElement;
                }

                return compactableNodeCount >= snapshotThresholdSize + snapshotBufferSizeFromLastEntry;
            }
        }

        public async Task<ISnapshotHeader> Compact(long commitIndex, long lastApplied, IEnumerable<INodeConfiguration> currentConfiguration, int snapshotThresholdSize, int snapshotBufferSizeFromLastEntry)
        {
            bool reachedThreshold = false;

            var entriesToMerge = new List<LogEntry>();

            lock (obj)
            {
                var currentElement = RootElement.NextElement;

                while (currentElement != null && currentElement.Entry.CurrentIndex <= lastApplied)
                {
                    entriesToMerge.Add(currentElement.Entry);

                    if (entriesToMerge.Count == snapshotThresholdSize)
                    {
                        reachedThreshold = true;
                        break;
                    }

                    currentElement = currentElement.NextElement;
                }
            }

            if (reachedThreshold)
            {
                var uncomittedHeader = await SnapshotManager.CreateFile(entriesToMerge, currentConfiguration);

                return uncomittedHeader;
            }

            return null;
        }

        internal Task<ISnapshotHeader> GetCurrentCommittedSnapshotFromLogs()
        {
            ISnapshotHeader currentCommittedSnapshot = null;

            lock (obj)
            {
                if (RootElement.NextElement != null && RootElement.NextElement.Entry.Type.HasFlag(LogEntry.Types.Snapshot))
                {
                    currentCommittedSnapshot = (ISnapshotHeader)RootElement.NextElement.Entry.Content;
                }
            }

            return Task.FromResult(currentCommittedSnapshot);
        }

        public async Task<bool> IsCommittedSnapshot(ISnapshotHeader snapshotDetail)
        {
            ISnapshotHeader currentCommittedSnapshot = await GetCurrentCommittedSnapshotFromLogs();

            if (await SnapshotManager.HasFile(snapshotDetail))
            {
                var file = await SnapshotManager.GetFile(snapshotDetail);

                return currentCommittedSnapshot.SnapshotId.Equals(snapshotDetail.SnapshotId)
                    && currentCommittedSnapshot.LastIncludedIndex.Equals(snapshotDetail.LastIncludedIndex)
                    && currentCommittedSnapshot.LastIncludedTerm.Equals(snapshotDetail.LastIncludedTerm);
            }

            return false;
        }

        public async Task<(ISnapshotDataChunk Data, bool IsLastOffset)> TryGetSnapshotChunk(ISnapshotHeader snapshotDetail, int offsetToFetch)
        {
            var file = await SnapshotManager.GetFile(snapshotDetail);

            if (await file.HasOffset(offsetToFetch))
            {
                var isLastOffset = await file.IsLastOffset(offsetToFetch);
                var chunkData = await file.ReadDataAt(offsetToFetch);

                return (chunkData, isLastOffset);
            }
            else
            {
                return (null, false);
            }
        }

        public async Task<bool> VerifySnapshot(ISnapshotHeader snapshotDetail, int lastOffset)
        {
            var file = await SnapshotManager.GetFile(snapshotDetail);

            return await file.Verify(lastOffset);
        }

        public async Task CommitAndUpdateLog(ISnapshotHeader snapshotDetail, bool replaceAll = false)
        {
            var newSnapshotEntry = new LogEntry
            {
                CurrentIndex = snapshotDetail.LastIncludedIndex,
                Term = snapshotDetail.LastIncludedTerm,
                Content = snapshotDetail,
                Type = LogEntry.Types.Snapshot
            };

            var snapshotElement = new Element
            {
                Entry = newSnapshotEntry,
            };

            await SnapshotManager.UpdateLastUsed(snapshotDetail);

            if (replaceAll)
            {
                lock (obj)
                {
                    //Update links such that the RootElement -> SnapshotElement, and abandon previous links
                    RootElement.NextElement = snapshotElement;

                    snapshotElement.NextElement = null;
                    snapshotElement.PreviousElement = RootElement;

                    LastElement = snapshotElement;
                }
            }
            else
            {
                lock (obj)
                {
                    var currentElement = RootElement;

                    //Find the element which the snapshot entry will connect to
                    while (currentElement != null)
                    {
                        if (currentElement.Entry.CurrentIndex > snapshotDetail.LastIncludedIndex)
                        {
                            break;
                        }

                        currentElement = currentElement.NextElement;
                    }

                    //Update links such that the RootElement -> SnapshotElement -> currentElement
                    snapshotElement.NextElement = currentElement;
                    currentElement.PreviousElement = snapshotElement;
                    snapshotElement.PreviousElement = RootElement;
                    RootElement.NextElement = snapshotElement;
                }
            }

            await SnapshotManager.OnlyKeep(snapshotDetail);

            SnapLog(MergedSnapshot);
        }

        public async Task<IEnumerable<NodeConfiguration>> GetConfigurationFromSnapshot(ISnapshotHeader snapshotDetail)
        {
            var file = await SnapshotManager.GetFile(snapshotDetail);

            var bytes = await file.ReadConfiguration();

            if (bytes == null)
                return null;

            var str = Encoding.UTF8.GetString(bytes);

            var config = JsonConvert.DeserializeObject<IEnumerable<NodeConfiguration>>(str);

            return config;
        }

        public async Task<ISnapshotHeader> FillSnapshotData(string snapshotId, long lastIncludedIndex, long lastIncludedTerm, int fillAtOffset, ISnapshotDataChunk receivedData)
        {
            ISnapshotFile file = null;
            ISnapshotHeader snapshot = null;

            var (HasFile, Detail) = await SnapshotManager.HasFile(snapshotId, lastIncludedIndex, lastIncludedTerm);

            if (!HasFile)
            {
                snapshot = await SnapshotManager.CreateFile(snapshotId, lastIncludedIndex, lastIncludedTerm);
            }

            file = await SnapshotManager.GetFile(snapshotId, lastIncludedIndex, lastIncludedTerm);

            await file.Fill(fillAtOffset, receivedData);

            return snapshot ?? Detail;
        }


        #endregion

        #region Log Holder


        private object obj = new object();
        public Element RootElement { get; private set; }
        public Element LastElement { get; private set; }

        public class SampleDisplayEntry
        {
            public object _ { get; init; }
            public long Term { get; init; }
            public long Index { get; set; }
            public string Type { get; init; }
        }

        private void SnapLog(string eventString)
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
                EntitySubject = EntityLog,
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
                SnapLog(OverwritingEntries);

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

                SnapLog(OverwrittenEntries);

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

        public Task<long?> GetLastIndexForTerm(long termNumber)
        {
            lock (obj)
            {
                var currentElement = LastElement;

                while (currentElement != null)
                {
                    if (currentElement.Entry.Term.Equals(termNumber))
                    {
                        return Task.FromResult((long?)currentElement.Entry.CurrentIndex);
                    }

                    currentElement = currentElement.PreviousElement;
                }

                return Task.FromResult(default(long?));
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
            long term = await GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    Term = term,
                    Content = inputCommand,
                    Type = LogEntry.Types.Command,
                };

                Append(item);

                SnapLog(AppendedCommand);

                return item;
            }
        }

        public async Task AppendNoOperationEntry()
        {
            long term = await GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    Term = term,
                    Content = null,
                    Type = LogEntry.Types.NoOperation,
                };

                Append(item);

                SnapLog(AppendNoOp);

                return;
            }
        }

        public async Task<LogEntry> AppendConfigurationEntry(IEnumerable<NodeConfiguration> configurations)
        {
            long term = await GetCurrentTerm();

            lock (obj)
            {
                LogEntry item = new LogEntry
                {
                    Term = term,
                    Content = configurations,
                    Type = LogEntry.Types.Configuration,
                };

                Append(item);

                SnapLog(AppendedConf);

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

        public Task<long> FetchLogEntryIndexPreviousToIndex(long index)
        {
            lock (obj)
            {
                var currentElement = RootElement;

                while (currentElement != null)
                {
                    if (currentElement.Entry.CurrentIndex.Equals(index))
                    {
                        break;
                    }

                    currentElement = currentElement.NextElement;
                }

                return Task.FromResult(currentElement.PreviousElement?.Entry?.CurrentIndex ?? default);
            }
        }

        #endregion

    }
}
