using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States
{
    /// <summary>
    /// 
    /// Terms act as a logical clock in Raft, and they allow servers to detect obsolete information such as stale leaders.
    /// Each server stores a current term number, which increases monotonically over time.
    /// <see cref="Section 5.1 Second-to-last Para"/>
    /// 
    /// 
    /// Holds the Persistent State actions of the Engine.
    /// 
    /// Current Term should be initialized to 0 and VotedFor should be cleared on initialization.
    /// </summary>
    public interface IPersistentProperties
    {
        #region Core 

        Task<long> GetCurrentTerm();

        Task SetCurrentTerm(long termNumber);

        Task<long> IncrementCurrentTerm();

        Task<string> GetVotedFor();

        Task SetVotedFor(string serverUniqueId);

        Task ClearVotedFor();

        #endregion

        #region Snapshots

        /// <summary>
        /// Determines if a committed snapshot is present, which includes the logEntry with the index <paramref name="inclusiveIndex"/>
        /// Returns back the committed snapshot's details which includes the logEntry with the index <paramref name="inclusiveIndex"/>
        /// </summary>
        /// <param name="inclusiveIndex"></param>
        /// <returns></returns>
        Task<(bool HasSnapshot, ISnapshotHeader SnapshotDetail)> HasCommittedSnapshot(long inclusiveIndex);

        /// <summary>
        /// Returns back the committed snapshot's details if present. If not, returns null;
        /// </summary>
        Task<ISnapshotHeader> GetCommittedSnapshot();


        /// <summary>
        /// Determines eligibility for background compaction
        /// </summary>
        /// <param name="commitIndex"></param>
        /// <param name="lastApplied"></param>
        /// <returns></returns>
        Task<bool> IsEligibleForCompaction(long commitIndex, long lastApplied, TimeSpan waitPeriod);

        /// <summary>
        /// Creates an uncommitted snapshot from the logEntries. 
        /// Holds the combined state from the existing snapshot (if any) and committed log entries (upto a certain index).
        /// Also includes the configuration used as of that snapshot.
        /// </summary>
        /// <param name="commitIndex"></param>
        /// <param name="lastApplied"></param>
        /// <returns>Uncommitted Snapshot's details</returns>
        Task<ISnapshotHeader> Compact(long commitIndex, long lastApplied);

        /// <summary>
        /// Returns the <see cref="ISnapshotChunkData"/> chunk data present in the snapshot at the offset <paramref name="offsetToFetch"/>.
        /// Returns <c>null</c>, if <paramref name="offsetToFetch"/> is out of bounds for <paramref name="snapshotDetail"/>
        /// Also, returns if the chunk is the last (i.e if <paramref name="offsetToFetch"/> is the last offset within the snapshot)
        /// </summary>
        /// <param name="snapshotDetail">Committed Snapshot Detail</param>
        /// <param name="offsetToFetch">Offset of the Snapshot</param>
        /// <returns>The ChunkData</returns>
        /// 
        Task<(ISnapshotChunkData Data, bool IsLastOffset)> TryGetSnapshotChunk(ISnapshotHeader snapshotDetail, int offsetToFetch);


        /// <summary>
        /// Determines if all the chunks of the snapshot have been received and duly filled
        /// </summary>
        /// <param name="snapshotDetail">Uncommitted snapshot detail</param>
        /// <param name="lastOffset">Last Offset</param>
        Task<bool> VerifySnapshot(ISnapshotHeader snapshotDetail, int lastOffset);

        /// <summary>
        /// Remove all snapshots having <see cref="ISnapshotHeader.LastIncludedIndex"/> and logEntries having Index, smaller than or equal to <paramref name="uptoIndex"/> and replace them with the snapshot <paramref name="replaceWith"/>
        /// 
        /// retain log entries (if any) following that entry and reply
        /// (for e.g say The follower has 0,1,2,3....54, then the last included term and entry matches for index 50, so the new log would look like 0, Snap[1 - 50], 51, 52, 53, 54)
        /// </summary>
        /// <param name="snapshotDetail">Uncommitted snapshot detail</param>
        /// <param name="replaceAll">If <c>true</c>, discards all entries and rebuild the log using <paramref name="snapshotDetail"/></param>
        /// <returns></returns>
        Task CommitAndUpdateLog(ISnapshotHeader snapshotDetail, bool replaceAll = false);

        /// <summary>
        /// Returns the configuration stated in the snapshot as of that <see cref="ISnapshotHeader.LastIncludedIndex"/>
        /// </summary>
        /// <param name="snapshotDetail"></param>
        /// <returns></returns>
        Task<IEnumerable<NodeConfiguration>> GetConfigurationFromSnapshot(ISnapshotHeader snapshotDetail);

        /// <summary>
        /// Using Copy-and-write, parallelly creates a new snapshot file, if not already present for the input parameters.
        /// Fills the snapshot with the <paramref name="receivedData"/> at the given <paramref name="fillAtOffset"/>. 
        /// </summary>
        /// <param name="fillAtOffset"></param>
        /// <param name="receivedData"></param>
        /// <returns>Created/Existing uncommitted snapshot</returns>
        Task<ISnapshotHeader> FillSnapshotData(string snapshotId, long lastIncludedIndex, long lastIncludedTerm, int fillAtOffset, ISnapshotChunkData receivedData);

        /// <summary>
        /// States that the committed <paramref name="snapshot"/> will be used for outbound <see cref="Remoting.RPC.InstallSnapshotRPC"/> calls
        /// </summary>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        Task MarkInstallation(ISnapshotHeader snapshot);

        /// <summary>
        /// States that the outbound <see cref="Remoting.RPC.InstallSnapshotRPC"/> calls for the committed <paramref name="snapshot"/> has completed
        /// </summary>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        Task MarkCompletion(ISnapshotHeader snapshot);

        #endregion

        #region Log Holder 


        /// <remarks>
        /// In Search of an Understandable Consensus Algorithm - (Extended Version) Diego Ongaro and John Ousterhout Stanford University <c>[Section 2]</c>
        /// 
        /// <para>
        /// Each server stores a log containing a series of commands, which its state machine executes in order.
        /// Each log contains the same commands in the same order, so each state machine processes the same sequence of commands.
        /// Since the state machines are deterministic, each computes the same state and the same sequence of outputs.
        /// </para>
        /// 
        /// <para>
        /// <c>Coracle.NET - </c>
        /// The log is a collection of individual <see cref="LogEntry"/>, containing a <see cref="LogEntry.Command"/>, <see cref="LogEntry.CurrentIndex"/> 
        /// and  <see cref="LogEntry.Term"/>. 
        /// 
        /// <see cref="IPersistentReplicatedLogHolder"/> should be implemented to simulate the persistent replicated log of the server.
        /// </para>
        /// </remarks>



        /// <summary>
        /// If a LogEntry is found at that <paramref name="index"/>, it is returned. Else, null is sent back.
        /// </summary>
        /// <param name="index"></param>
        Task<LogEntry> TryGetValueAtIndex(long index);

        /// <summary>
        /// Returns Last Log Entry
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Lo</returns>
        Task<LogEntry> TryGetValueAtLastIndex();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Task<long> GetTermAtIndex(long index);

        /// <summary>
        /// Returns the last Index of the log persisted.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Lo</returns>
        Task<long> GetLastIndex();

        /// <summary>
        /// Returns the last Index of the log persisted for the given Term.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Lo</returns>
        Task<long> GetLastIndexForTerm(long termNumber);

        /// <summary>
        /// Returns the first Index of the log persisted for the given Term.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Nullable if Term not found. Else returns the value of the Index</returns>
        Task<long?> GetFirstIndexForTerm(long termNumber);

        /// <summary>
        /// Returns true if the given Term exists. False otherwise.
        /// </summary>
        /// <param name="index"></param>
        Task<bool> DoesTermExist(long termNumber);

        /// <summary>
        /// Returns the nearest, smaller valid term value  
        /// </summary>
        /// <param name="invalidTerm">The Invalid Term number</param>
        Task<long> FindValidTermPreviousTo(long invalidTerm);

        /// <summary>
        /// If entry term and index match existing logentry, we can skip. Else we can append/overwrite.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="logEntries"></param>
        /// <returns></returns>
        Task OverwriteEntries(IEnumerable<LogEntry> logEntries);

        /// <remarks>
        /// Each log entry stores a state machine command along with the term number when the entry was received by the leader.
        /// The term numbers in log entries are used to detect inconsistencies between logs and to ensure some of the properties
        /// in Figure 3. Each log entry also has an integer index identifying its position in the log.
        /// <seealso cref="Section 5.3 Log Replication"/>
        /// </remarks>
        /// <summary>
        /// Extension logic.
        /// Creates a new LogEntry with the Input Command is not already present.
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="inputCommand"></param>
        Task<LogEntry> AppendNewCommandEntry<TCommand>(TCommand inputCommand) where TCommand : class, ICommand; //where TCommandResult : ClientHandlingResult, new();
        Task<long> FetchLogEntryIndexPreviousToIndex(long index);

        /// <remarks>
        /// Each log entry stores a state machine command along with the term number when the entry was received by the leader.
        /// The term numbers in log entries are used to detect inconsistencies between logs and to ensure some of the properties
        /// in Figure 3. Each log entry also has an integer index identifying its position in the log.
        /// <seealso cref="Section 5.3 Log Replication"/>
        /// </remarks>
        /// <summary>
        /// Extension logic.
        /// Creates a new LogEntry with the Input Command is not already present.
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="inputCommand"></param>
        Task<LogEntry> AppendConfigurationEntry(IEnumerable<NodeConfiguration> configurations);

        Task<IEnumerable<NodeConfiguration>> ReadFrom(LogEntry configurationLogEntry);
        Task<ICommand> ReadFrom<ICommand>(LogEntry commandLogEntry);
        Task AppendNoOperationEntry();

        /// <summary>
        /// Fetches the LogEntries from [startIndex, endIndex] i.e inclusive
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="EndIndex"></param>
        /// <returns></returns>
        Task<IEnumerable<LogEntry>> FetchLogEntriesBetween(long startIndex, long endIndex);

        [Obsolete(message: "Not used during core RAFT processes. Can be used for logging")]
        IEnumerable<LogEntry> GetAll();
        Task<bool> IsInputSnapshotCommittedAndAvailableInLog(ISnapshotHeader snapshotDetail);


        #endregion
    }
}
