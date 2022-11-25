using System;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.States.LeaderState
{
    /// <summary>
    /// Central Outbound AppendEntries RPC manager
    /// </summary>
    public interface IAppendEntriesManager 
    {
        /// <summary>
        /// The heartbeat timer can call this method to send outbound AppendEntries RPC to communicate with other nodes
        /// This can also be called from Client Command Processing, i.e whenever a client Command is written to the log, and needs replication
        /// </summary>
        void InitiateAppendEntries();
        bool CanSendTowards(string uniqueNodeId);
        void IssueRetry(string uniqueNodeId);
        void UpdateFor(string uniqueNodeId);
        Dictionary<string, DateTimeOffset> FetchLastPinged();
    }
}
