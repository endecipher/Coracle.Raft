namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IAppendEntriesRPCResponse : IRemoteResponse
    {
        long? ConflictingEntryTermOnFailure { get; }
        long? FirstIndexOfConflictingEntryTermOnFailure { get; }
        bool Success { get; }
        long Term { get; }
    }

    public class AppendEntriesRPCResponse : IAppendEntriesRPCResponse
    {
        public long Term { get; set; }

        public bool Success { get; set; }

        public long? ConflictingEntryTermOnFailure { get; set; }

        public long? FirstIndexOfConflictingEntryTermOnFailure { get; set; }
    }

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
}