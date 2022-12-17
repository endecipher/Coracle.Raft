using System;

namespace Core.Raft.Canoe.Engine.Remoting.RPC
{
    [Serializable]
    public class AppendEntriesRPCResponse : IAppendEntriesRPCResponse
    {
        public long Term { get; init; }

        public bool Success { get; init; }

        /// <remarks>
        /// The following two additional properties are with respect to the below passage.
        /// 
        /// If desired, the protocol can be optimized to reduce the number of rejected AppendEntries RPCs.
        /// For example,when rejecting an AppendEntries request, the follower can include the term of the conflicting entry and the 
        /// first index it stores for that term. 
        /// 
        /// With this information, the leader can decrement nextIndex to bypass all of the conflicting entries in that term; 
        /// one AppendEntries RPC will be required for each term with conflicting entries, rather than one RPC per entry.
        /// <seealso cref="Section 5.3 Log Replication"/>
        /// </remarks>
        public long? ConflictingEntryTermOnFailure { get; init; }

        public long? FirstIndexOfConflictingEntryTermOnFailure { get; init; }
    }
}