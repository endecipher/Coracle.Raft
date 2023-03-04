using Coracle.Raft.Engine.Configuration.Cluster;
using System;

namespace Coracle.Raft.Engine.Logs
{
    public sealed class LogEntry
    {
        #region Use-case based
        public object Content { get; set; }

        #endregion

        public long Term { get; init; }
        public long CurrentIndex { get; set; }
        public Types Type { get; set; }

        [Flags]
        public enum Types
        {
            /// <summary>
            /// Usually used as a starting/first and only entry across all nodes
            /// </summary>
            None = 1,

            /// <summary>
            /// Signifies Contents as Empty. Used during No-Operation Log Entries for whenever a <see cref="States.Leader"/> is established
            /// </summary>
            NoOperation = 2,

            /// <summary>
            /// Signifies that the underlying Contents denote a <see cref="Command.ICommand"/> to be executed
            /// </summary>
            Command = 4,

            /// <summary>
            /// Signifies that the underlying Contents are actually an enumerable of <see cref="NodeConfiguration"/> 
            /// </summary>
            Configuration = 8,

            /// <summary>
            /// Signifies that the underlying Contents are actually that of a snapshot, and that the logEntry's Index and Term indicate the <c>LastIncludedIndex</c> and <c>LastIncludedTerm</c>
            /// </summary>
            Snapshot = 16
        }
    }
}
