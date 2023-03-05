using Coracle.Raft.Engine.Command;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Engine.Snapshots;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States
{
    /// <summary>
    /// Holds the Persistent State. 
    /// Current Term should be initialized to 0 and VotedFor should be cleared on initialization.
    /// </summary>
    public interface IPersistentStateHandler
    {
        #region CurrentTerm and VotedFor manipulations 

        /// <summary>
        /// Returns the Current Term.
        /// </summary>
        Task<long> GetCurrentTerm();

        /// <summary>
        /// Updates the Current Term to <paramref name="termNumber"/>
        /// </summary>
        Task SetCurrentTerm(long termNumber);

        /// <summary>
        /// Updates the the Current Term to the incremented value by 1
        /// </summary>
        Task<long> IncrementCurrentTerm();

        /// <summary>
        /// Returns the <see cref="Node.IEngineConfiguration.NodeId"/> of another node in the cluster for whom this Coracle node voted for
        /// </summary>
        Task<string> GetVotedFor();

        /// <summary>
        /// Updates VotedFor to <paramref name="serverUniqueId"/>
        /// </summary>
        Task SetVotedFor(string serverUniqueId);

        /// <summary>
        /// Updates VotedFor to a null value
        /// </summary>
        Task ClearVotedFor();

        #endregion


        #region Snapshots

        /// <summary>
        /// Returns back the current committed snapshot's details which is present in the <see cref="LogEntry"/> having the 
        /// type <see cref="LogEntry.Types.Snapshot"/>
        /// </summary>
        /// <returns>Returns back the current committed snapshot's <see cref="ISnapshotHeader"/> details if found. Else <c>null</c> is returned</returns>
        Task<ISnapshotHeader> GetCommittedSnapshot();

        /// <summary>
        /// Determines whether the current committed snapshot's details exactly match that of <paramref name="snapshotDetail"/>
        /// </summary>
        /// <returns><c>true</c> if the current committed snapshot's <see cref="ISnapshotHeader"/> properties match <paramref name="snapshotDetail"/>'s properties. <c>false</c> otherwise</returns>
        Task<bool> IsCommittedSnapshot(ISnapshotHeader snapshotDetail);

        /// <summary>
        /// Determines eligibility for compaction of logEntries.
        /// '<paramref name="snapshotThresholdSize"/>' number of logEntries must exist for merge/compaction, and an extra '<paramref name="snapshotBufferSizeFromLastEntry"/>' number of logEntries must be kept as a buffer from the <paramref name="lastApplied"/> logEntry.
        /// </summary>
        /// <param name="commitIndex">Node's commit index</param>
        /// <param name="lastApplied">Node's lastApplied entry index</param>
        /// <param name="snapshotThresholdSize">Number of entries to merge/compact</param>
        /// <param name="snapshotBufferSizeFromLastEntry">Number of entries to be left out of compaction which are earlier than the <paramref name="lastApplied"/> indexed entry</param>
        /// <param name="waitPeriod"><see cref="TimeSpan"/> which must denote the minimum time elapsed since the last usage of the current committed snapshot (if any)</param>
        /// <returns><c>true</c> if compaction can be triggered. <c>false</c> otherwise</returns>
        Task<bool> IsEligibleForCompaction(long commitIndex, long lastApplied, TimeSpan waitPeriod, int snapshotThresholdSize, int snapshotBufferSizeFromLastEntry);

        /// <summary>
        /// Creates a new uncommitted snapshot from merging the first '<see cref="Node.IEngineConfiguration.SnapshotThresholdSize"/>' number of logEntries and returns the corresponding <see cref="ISnapshotHeader"/>.
        /// The snapshot will hold the combined state of all the merged entries; inclusive of the state of the existing committed snapshot (i.e if a logEntry of type <see cref="LogEntry.Types.Snapshot"/> exists).
        /// The snapshot will also include the current configuration denoted by <paramref name="currentConfiguration"/>.
        /// </summary>
        /// <param name="commitIndex">Node's commit index</param>
        /// <param name="lastApplied">Node's lastApplied entry index</param>
        /// <param name="currentConfiguration">Current Cluster configuration to include in the snapshot</param>
        /// <param name="snapshotThresholdSize">Number of entries to merge/compact</param>
        /// <param name="snapshotBufferSizeFromLastEntry">Number of entries to be left out of compaction which are earlier than the <paramref name="lastApplied"/> indexed entry</param>
        /// <returns>Uncommitted Snapshot's details</returns>
        Task<ISnapshotHeader> Compact(long commitIndex, long lastApplied, IEnumerable<INodeConfiguration> currentConfiguration, int snapshotThresholdSize, int snapshotBufferSizeFromLastEntry);

        /// <summary>
        /// Returns the <see cref="ISnapshotDataChunk"/> chunk data present in the snapshot pointed by <paramref name="snapshotDetail"/> 
        /// at the offset <paramref name="offsetToFetch"/>.
        /// </summary>
        /// <param name="snapshotDetail">Committed Snapshot Detail</param>
        /// <param name="offsetToFetch">Offset of the Snapshot</param>
        /// <returns>
        /// Returns the snapshot's data chunk present at the offset <paramref name="offsetToFetch"/>; if <paramref name="offsetToFetch"/> is out of bounds, returns <see cref="ISnapshotDataChunk"/> as <c>null</c> for the <paramref name="snapshotDetail"/>. 
        /// Also, returns whether <paramref name="offsetToFetch"/> is the last offset within the snapshot i.e the last chunk of data. 
        /// </returns>
        Task<(ISnapshotDataChunk Data, bool IsLastOffset)> TryGetSnapshotChunk(ISnapshotHeader snapshotDetail, int offsetToFetch);

        /// <summary>
        /// Determines if all the chunks of the uncommitted snapshot have been received and duly filled
        /// </summary>
        /// <param name="snapshotDetail">Uncommitted snapshot details</param>
        /// <param name="lastOffset">Last Offset</param>
        /// <returns><c>true</c> if all chunks are received and present. <c>false</c> otherwise</returns>
        Task<bool> VerifySnapshot(ISnapshotHeader snapshotDetail, int lastOffset);

        /// <summary>
        /// Removes all logEntries having <see cref="LogEntry.CurrentIndex"/> smaller than or equal to <see cref="ISnapshotHeader.LastIncludedIndex"/> of <paramref name="snapshotDetail"/>, 
        /// and replaces them with a new logEntry of type <see cref="LogEntry.Types.Snapshot"/>, with the <see cref="LogEntry.CurrentIndex"/> as the <see cref="ISnapshotHeader.LastIncludedIndex"/> 
        /// and the <see cref="LogEntry.Term"/> as the <see cref="ISnapshotHeader.LastIncludedTerm"/> of the passed <paramref name="snapshotDetail"/>.
        /// 
        /// Since the <paramref name="snapshotDetail"/> will now be the only committed snapshot present in the replicated log, all uncommitted snapshots having
        /// <see cref="ISnapshotHeader.LastIncludedIndex"/> lesser than or equal to that of the lastIncludedIndex of <paramref name="snapshotDetail"/> can be removed.
        /// </summary>
        /// <param name="snapshotDetail">Snapshot detail declared to be committed</param>
        /// <param name="replaceAll">If <c>true</c>, discards all entries and initializes the log using <paramref name="snapshotDetail"/>. If <c>false</c>, logEntries following the lastIncludedIndex (if any), are retained</param>
        Task CommitAndUpdateLog(ISnapshotHeader snapshotDetail, bool replaceAll = false);

        /// <summary>
        /// Returns the configuration embedded in the snapshot pointed by <paramref name="snapshotDetail"/>
        /// </summary>
        /// <param name="snapshotDetail">Snapshot detail</param>
        /// <returns>Cluster Configuration embedded in the snapshot pointed <paramref name="snapshotDetail"/></returns>
        Task<IEnumerable<NodeConfiguration>> GetConfigurationFromSnapshot(ISnapshotHeader snapshotDetail);

        /// <summary>
        /// Using Copy-and-write, parallelly creates a new snapshot file if not already present for the parameters: <paramref name="snapshotId"/>, <paramref name="lastIncludedIndex"/> 
        /// and <paramref name="lastIncludedTerm"/>.
        /// Fills the snapshot with the <paramref name="receivedData"/> at the given <paramref name="fillAtOffset"/>. 
        /// </summary>
        /// <param name="snapshotId">Received unique snapshot's id which corresponds to <see cref="ISnapshotHeader.SnapshotId"/></param>
        /// <param name="lastIncludedIndex">Received snapshot's lastIncludedEntry's index which corresponds to <see cref="ISnapshotHeader.LastIncludedIndex"/></param>
        /// <param name="lastIncludedTerm">Received snapshot's lastIncludedEntry's term which corresponds to <see cref="ISnapshotHeader.LastIncludedTerm"/></param>
        /// <param name="fillAtOffset">Underlying offset towards which the <paramref name="receivedData"/> must be stored</param>
        /// <param name="receivedData">Received Data to be stored</param>
        /// <returns>Newly created or existing uncommitted snapshot header details</returns>
        Task<ISnapshotHeader> FillSnapshotData(string snapshotId, long lastIncludedIndex, long lastIncludedTerm, int fillAtOffset, ISnapshotDataChunk receivedData);

        /// <summary>
        /// States that the committed <paramref name="snapshot"/> will be used for outbound <see cref="Remoting.RPC.InstallSnapshotRPC"/> calls.
        /// Usage can be tracked for <see cref="Node.IEngineConfiguration.CompactionWaitPeriod_InMilliseconds"/>
        /// </summary>
        /// <param name="snapshot">Snapshot details</param>
        Task MarkInstallation(ISnapshotHeader snapshot);

        /// <summary>
        /// States that the outbound <see cref="Remoting.RPC.InstallSnapshotRPC"/> calls for the committed <paramref name="snapshot"/> has completed.
        /// Usage can be tracked for <see cref="Node.IEngineConfiguration.CompactionWaitPeriod_InMilliseconds"/>
        /// </summary>
        /// <param name="snapshot">Snapshot details</param>
        Task MarkCompletion(ISnapshotHeader snapshot);

        #endregion


        #region Log Entries / Log Entry Chain

        /// <summary>
        /// Tries to find a <see cref="LogEntry"/> with <see cref="LogEntry.CurrentIndex"/> as <paramref name="index"/>
        /// </summary>
        /// <param name="index">LogEntry's matching index</param>
        /// <returns>If a logEntry is found which has <see cref="LogEntry.CurrentIndex"/> as <paramref name="index"/>, it is returned. Else, null is sent back.</returns>
        Task<LogEntry> TryGetValueAtIndex(long index);

        /// <summary>
        /// Returns the last <see cref="LogEntry"/> present in the log chain
        /// </summary>
        /// <returns>logEntry found having the highest <see cref="LogEntry.CurrentIndex"/>. Must always return a <see cref="LogEntry"/>; cannot return back <c>null</c></returns>
        Task<LogEntry> TryGetValueAtLastIndex();

        /// <summary>
        /// Returns <see cref="LogEntry.CurrentIndex"/> of the last <see cref="LogEntry"/> present in the log chain
        /// </summary>
        /// <returns><see cref="LogEntry.CurrentIndex"/> of the last <see cref="LogEntry"/></returns>
        Task<long> GetLastIndex();

        /// <summary>
        /// Returns <see cref="LogEntry.CurrentIndex"/> of the last <see cref="LogEntry"/> present in the log chain having <see cref="LogEntry.Term"/> as <paramref name="termNumber"/>
        /// </summary>
        /// <param name="termNumber">Term Number</param>
        /// <returns><see cref="LogEntry.CurrentIndex"/> of the found entry. <c>null</c> if not found</returns>
        Task<long?> GetLastIndexForTerm(long termNumber);

        /// <summary>
        /// Returns <see cref="LogEntry.CurrentIndex"/> of the first <see cref="LogEntry"/> present in the log chain having <see cref="LogEntry.Term"/> as <paramref name="termNumber"/>
        /// </summary>
        /// <param name="termNumber">Term Number</param>
        /// <returns><see cref="LogEntry.CurrentIndex"/> of the found entry. <c>null</c> if not found</returns>
        Task<long?> GetFirstIndexForTerm(long termNumber);

        /// <summary>
        /// Checks if the logChain contains any <see cref="LogEntry"/> having <see cref="LogEntry.Term"/> as <paramref name="termNumber"/>
        /// </summary>
        /// <param name="termNumber">Term Number</param>
        /// <returns><c>true</c> if term exists. <c>false</c> otherwise</returns>
        Task<bool> DoesTermExist(long termNumber);

        /// <summary>
        /// Finds the valid term smaller than, but nearest to the supplied <paramref name="invalidTerm"/> for which a <see cref="LogEntry"/> exists 
        /// </summary>
        /// <param name="invalidTerm">Invalid Term Number</param>
        /// <returns>Returns a valid <see cref="LogEntry.Term"/> previous to <paramref name="invalidTerm"/></returns>
        Task<long> FindValidTermPreviousTo(long invalidTerm);

        /// <summary>
        /// Replaces/Overwrites all existing entries in the log chain with <paramref name="entriesToReplace"/>; replacement starts from 
        /// the <see cref="LogEntry"/> whose <see cref="LogEntry.Term"/> and <see cref="LogEntry.CurrentIndex"/> 
        /// matches the first entry of the supplied set <paramref name="entriesToReplace"/>
        /// </summary>
        /// <param name="entriesToReplace">Set of <see cref="LogEntry"/> to replace</param>
        Task OverwriteEntries(IEnumerable<LogEntry> entriesToReplace);


        /// <summary>
        /// Appends a new <see cref="LogEntry"/> of type <see cref="LogEntry.Types.Command"/> having <see cref="LogEntry.Content"/> as the <paramref name="inputCommand"/>.
        /// <see cref="LogEntry.Term"/> and <see cref="LogEntry.CurrentIndex"/> are the <see cref="GetCurrentTerm"/> and the (<see cref="GetLastIndex"/> + 1) respectively.
        /// </summary>
        /// <typeparam name="TCommand">Extensible Command Implementation type</typeparam>
        /// <param name="inputCommand">Command object</param>
        /// <returns>The newly created and appended <see cref="LogEntry"/></returns>
        Task<LogEntry> AppendNewCommandEntry<TCommand>(TCommand inputCommand) where TCommand : class, ICommand;

        /// <summary>
        /// Fetches the <see cref="LogEntry.CurrentIndex"/> of the <see cref="LogEntry"/> which is immediately previous to the <see cref="LogEntry"/> having <see cref="LogEntry.CurrentIndex"/> as <paramref name="index"/>
        /// </summary>
        /// <param name="index">Log Entry Index</param>
        /// <returns><see cref="LogEntry.CurrentIndex"/> of the previous <see cref="LogEntry"/></returns>
        Task<long> FetchLogEntryIndexPreviousToIndex(long index);

        /// <summary>
        /// Appends a new <see cref="LogEntry"/> of type <see cref="LogEntry.Types.Configuration"/> having <see cref="LogEntry.Content"/> as the <paramref name="configurations"/>.
        /// <see cref="LogEntry.Term"/> and <see cref="LogEntry.CurrentIndex"/> are the <see cref="GetCurrentTerm"/> and the (<see cref="GetLastIndex"/> + 1) respectively.
        /// </summary>
        /// <param name="configurations">cluster configuration at a given point</param>
        /// <returns>The newly created and appended <see cref="LogEntry"/></returns>
        Task<LogEntry> AppendConfigurationEntry(IEnumerable<NodeConfiguration> configurations);

        /// <summary>
        /// Extracts the cluster configuration from the supplied <paramref name="configurationLogEntry"/>
        /// </summary>
        /// <param name="configurationLogEntry">A configuration <see cref="LogEntry"/> (having type <see cref="LogEntry.Types.Configuration"/>)</param>
        /// <returns>Cluster configuration read from the <see cref="LogEntry.Content"/> of the supplied <paramref name="configurationLogEntry"/></returns>
        Task<IEnumerable<NodeConfiguration>> ReadFrom(LogEntry configurationLogEntry);

        /// <summary>
        /// Extracts the command <see cref="ICommand"/> from the supplied <paramref name="commandLogEntry"/>
        /// </summary>
        /// <typeparam name="ICommand">Base Command Type</typeparam>
        /// <param name="commandLogEntry">A command <see cref="LogEntry"/> (having type <see cref="LogEntry.Types.Command"/>)</param>
        /// <returns>Command object read from the <see cref="LogEntry.Content"/> of the supplied <paramref name="commandLogEntry"/></returns>
        Task<ICommand> ReadFrom<ICommand>(LogEntry commandLogEntry);

        /// <summary>
        /// Appends a new <see cref="LogEntry"/> of type <see cref="LogEntry.Types.NoOperation"/> having blank/empty <see cref="LogEntry.Content"/>.
        /// <see cref="LogEntry.Term"/> and <see cref="LogEntry.CurrentIndex"/> are the <see cref="GetCurrentTerm"/> and the (<see cref="GetLastIndex"/> + 1) respectively.
        /// </summary>
        Task AppendNoOperationEntry();

        /// <summary>
        /// Fetches multiple <see cref="LogEntry"/> whose <see cref="LogEntry.CurrentIndex"/> lie in the inclusive range [<paramref name="startIndex"/>, <paramref name="endIndex"/>] 
        /// i.e inclusive of the <paramref name="startIndex"/> and <paramref name="endIndex"/>
        /// </summary>
        /// <param name="startIndex">Starting <see cref="LogEntry.CurrentIndex"/></param>
        /// <param name="endIndex">End <see cref="LogEntry.CurrentIndex"/></param>
        /// <returns>Set of <see cref="LogEntry"/></returns>
        Task<IEnumerable<LogEntry>> FetchLogEntriesBetween(long startIndex, long endIndex);

        #endregion
    }

    /// <remarks>
    /// Terms act as a logical clock in Raft, and they allow servers to detect obsolete information such as stale leaders.
    /// Each server stores a current term number, which increases monotonically over time.
    /// <see cref="Section 5.1 Second-to-last Para"/>
    /// </remarks>

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
    /// </para>
    /// </remarks>

    /// <remarks>
    /// <see cref="Coracle.Raft.Engine.States.IPersistentStateHandler.CommitAndUpdateLog(ISnapshotHeader, bool)"/>
    /// 
    /// retain log entries (if any) following that entry and reply
    /// (for e.g say The follower has 0,1,2,3....54, then the last included term and entry matches for index 50, so the new log would look like 0, Snap[1 - 50], 51, 52, 53, 54)
    /// </remarks>
    /// 


    /// <remarks>
    /// <see cref="Coracle.Raft.Engine.States.IPersistentStateHandler.AppendNewCommandEntry{TCommand}(TCommand)"/>
    /// 
    /// Each log entry stores a state machine command along with the term number when the entry was received by the leader.
    /// The term numbers in log entries are used to detect inconsistencies between logs and to ensure some of the properties
    /// in Figure 3. Each log entry also has an integer index identifying its position in the log.
    /// <seealso cref="Section 5.3 Log Replication"/>
    /// </remarks>
}

