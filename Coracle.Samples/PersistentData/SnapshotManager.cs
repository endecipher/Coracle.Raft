using ActivityLogger.Logging;
using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Actions.Core;
using System.Collections.Concurrent;
using Coracle.Samples.ClientHandling.Notes;
using Coracle.Samples.ClientHandling.NoteCommand;
using Newtonsoft.Json;
using System.Text;

namespace Coracle.Samples.PersistentData
{
    public class SnapshotManager : ISnapshotManager
    {
        public class Snap
        {
            public ISnapshotFile File { get; set; }
            public ISnapshotHeader Detail { get; set; }
            public DateTimeOffset LastUsed { get; set; } 
        }

        ConcurrentDictionary<(string SnapshotId, long LastIncludedIndex, long lastIncludedTerm), Snap> Map { get; }
        public IActivityLogger ActivityLogger { get; }

        public SnapshotManager(IActivityLogger activityLogger)
        {
            Map = new ConcurrentDictionary<(string SnapshotId, long LastIncludedIndex, long lastIncludedTerm), Snap>();

            ActivityLogger = activityLogger;
        }

        public async Task OnlyKeep(ISnapshotHeader compactedSnapshotHeader)
        {
            var id = compactedSnapshotHeader.SnapshotId;
            var index = compactedSnapshotHeader.LastIncludedIndex;
            var term = compactedSnapshotHeader.LastIncludedTerm;

            var file = await GetFile(compactedSnapshotHeader);

            await file.Complete();

            foreach (var key in Map.Keys)
            {
                if (key.SnapshotId.Equals(id) && key.LastIncludedIndex.Equals(index) && key.lastIncludedTerm.Equals(term))
                {
                    continue;
                }

                Map.Remove(key, out var val);
            }
        }

        public Task<ISnapshotFile> GetFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm)
        {
            if (Map.TryGetValue((snapshotId, lastIncludedIndex, lastIncludedTerm), out var value))
            {
                return Task.FromResult(value.File);
            }

            throw new IndexOutOfRangeException($"Snapshot with Id:{snapshotId} Index:{lastIncludedIndex} Term:{lastIncludedTerm} doesn't exist");
        }

        public Task<ISnapshotFile> GetFile(ISnapshotHeader detail)
        {
            if (Map.TryGetValue((detail.SnapshotId, detail.LastIncludedIndex, detail.LastIncludedTerm), out var value))
            {
                return Task.FromResult(value.File);
            }

            throw new IndexOutOfRangeException($"Snapshot with Id:{detail.SnapshotId} Index:{detail.LastIncludedIndex} Term:{detail.LastIncludedTerm} doesn't exist");
        }

        public Task<(bool HasFile, ISnapshotHeader Detail)> HasFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm)
        {
            bool isPresent = Map.TryGetValue((snapshotId, lastIncludedIndex, lastIncludedTerm), out var value);

            return Task.FromResult((isPresent, value?.Detail));
        }

        public Task<bool> HasFile(ISnapshotHeader detail)
        {
            bool isPresent = Map.TryGetValue((detail.SnapshotId, detail.LastIncludedIndex, detail.LastIncludedTerm), out var value);

            return Task.FromResult(isPresent);
        }

        public async Task<ISnapshotHeader> CreateFile(IEnumerable<LogEntry> logEntries, IEnumerable<INodeConfiguration> currentNodeConfigurations)
        {
            var newSnapshotFile = new File();

            var newState = new Notes(ActivityLogger);

            //Build state and keep track of last Config Entry if present
            foreach (var logEntry in logEntries)
            {
                if (logEntry.Type.HasFlag(LogEntry.Types.Snapshot))
                {
                    //Existing Snapshot being merged with further entries

                    var existingDetail = logEntry.Content as ISnapshotHeader;
                    var existingFile = await GetFile(existingDetail);
                    newState.Build(await existingFile.ReadData());
                }

                if (logEntry.Type.HasFlag(LogEntry.Types.Command))
                {
                    var logEntryCommand = logEntry.Content as ICommand;

                    if (logEntryCommand == null || logEntryCommand.IsReadOnly || !logEntryCommand.Type.Equals(Notes.AddNote))
                    {
                        continue;
                    }

                    if (logEntryCommand is NoteCommand baseNoteCommand)
                    {
                        newState.Add(baseNoteCommand.Data);
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid Command Type during conversion and building state");
                    }
                }
            }

            byte[] data = newState.ExportData();

            int offset = await newSnapshotFile.GetLastOffset();

            //Append data to file in chunks
            foreach (var chunk in data.Chunk(File.MaxBytesInChunk))
            {
                await newSnapshotFile.Fill(offset++, new DataChunk
                {
                    Data = chunk
                });
            }

            var configurationString = JsonConvert.SerializeObject(currentNodeConfigurations);

            byte[] configByteArr = Encoding.ASCII.GetBytes(configurationString);
            
            //Append latest Configuration Data
            await newSnapshotFile.AppendConfiguration(configByteArr);

            var newSnapshotDetail = new SnapshotDetail
            {
                LastIncludedIndex = logEntries.Last().CurrentIndex,
                LastIncludedTerm = logEntries.Last().Term,
                SnapshotId = Guid.NewGuid().ToString(),
            };

            var snap = new Snap
            {
                File = newSnapshotFile,
                Detail = newSnapshotDetail,
                LastUsed = DateTimeOffset.Now
            };

            Map.AddOrUpdate((newSnapshotDetail.SnapshotId, newSnapshotDetail.LastIncludedIndex, newSnapshotDetail.LastIncludedTerm), snap, (mergedKey, oldKey) =>
            {
                return snap;
            });

            await newSnapshotFile.Complete();

            return newSnapshotDetail;
        }

        public Task<ISnapshotHeader> CreateFile(string snapshotId, long lastIncludedIndex, long lastIncludedTerm)
        {
            var newSnapshotFile = new File();

            var newSnapshotDetail = new SnapshotDetail
            {
                LastIncludedIndex = lastIncludedIndex,
                LastIncludedTerm = lastIncludedTerm,
                SnapshotId = snapshotId,
            };

            var snap = new Snap
            {
                File = newSnapshotFile,
                Detail = newSnapshotDetail,
                LastUsed = DateTimeOffset.Now
            };

            Map.AddOrUpdate((snapshotId, lastIncludedIndex, lastIncludedTerm), snap, (mergedKey, oldKey) =>
            {
                return snap;
            });

            return Task.FromResult<ISnapshotHeader>(newSnapshotDetail);
        }

        public Task UpdateLastUsed(ISnapshotHeader detail)
        {
            if (Map.TryGetValue((detail.SnapshotId, detail.LastIncludedIndex, detail.LastIncludedTerm), out var value))
            {
                value.LastUsed = DateTimeOffset.Now;
            }

            return Task.CompletedTask;
        }

        public Task<DateTimeOffset> GetLastUsed(ISnapshotHeader detail)
        {
            if (Map.TryGetValue((detail.SnapshotId, detail.LastIncludedIndex, detail.LastIncludedTerm), out var value))
            {
                return Task.FromResult(value.LastUsed);
            }

            return Task.FromResult(DateTimeOffset.MinValue);
        }
    }
}
