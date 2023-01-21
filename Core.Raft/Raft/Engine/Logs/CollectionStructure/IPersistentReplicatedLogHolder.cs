using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Configuration.Cluster;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Logs.CollectionStructure
{
    /// <summary>
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
    /// </summary>
    public interface IPersistentReplicatedLogHolder
    {
        /// <summary>
        /// If a LogEntry is found at that <paramref name="index"/> , it is returned. Else, null is sent back.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Lo</returns>
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
    }
}
