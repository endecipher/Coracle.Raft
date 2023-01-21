using Coracle.Raft.Engine.Configuration.Cluster;
using System;

namespace Coracle.Raft.Engine.Logs
{

    /// <summary>
    /// In Search of an Understandable Consensus Algorithm - (Extended Version) Diego Ongaro and John Ousterhout Stanford University <c>[Section 4 - End Para]</c>
    /// 
    /// <para>
    /// Our second approach was to simplify the state space by reducing the number of states to consider, making the system more coherent and 
    /// eliminating nondeterminism where possible. Specifically, logs are not allowed to have holes, and Raft limits the ways in which logs can become
    /// inconsistent with each other.
    /// </para>
    /// 
    /// <para>
    /// <c>Coracle.NET - </c>
    /// The log is a collection of individual <see cref="LogEntry"/>, containing a <see cref="LogEntry.Command"/>, <see cref="CurrentIndex"/> 
    /// and  <see cref="Term"/>. 
    /// 
    /// <see cref="IPersistentReplicatedLogHolder"/> which deals with LogEntries
    /// </para>
    /// </summary>
    /// 

    public sealed class LogEntry
    {
        public object Contents { get; init; }
        public long Term { get; init; }
        public long CurrentIndex { get; set; }
        public Types Type { get; init; }

        [Flags]
        public enum Types
        {
            /// <summary>
            /// No contributions - empty in all fields
            /// </summary>
            None = 0,

            /// <summary>
            /// Signifies Contents as Empty.
            /// Used during No-Operation Log Entries for whenever a <see cref="States.Leader"/> is established.
            /// </summary>
            NoOperation = 1,

            /// <summary>
            /// Signifies that the underlying Contents denote a <see cref="ClientHandling.Command.ICommand"/> to be executed
            /// </summary>
            Command = 2,

            /// <summary>
            /// Signifies that the underlying Contents are actually an enumerable of <see cref="NodeConfiguration"/> 
            /// </summary>
            Configuration = 4,
        }
    }

}
