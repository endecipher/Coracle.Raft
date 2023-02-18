using Coracle.Raft.Engine.Actions.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States.LeaderEntities
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
        void Initialize();
        bool CanSendTowards(string uniqueNodeId);
        void UpdateFor(string uniqueNodeId);
        Dictionary<string, DateTimeOffset> FetchLastPinged();
        
        void InitiateAppendEntries();
        void IssueRetry(string uniqueNodeId);
        
        void IssueRetry(string uniqueNodeId, int failedOffset);
        Task SendNextSnapshotChunk(string uniqueNodeId, int successfulOffset, bool resendAll = false);
        Task InitiateSnapshotInstallation(string uniqueNodeId, ISnapshotHeader snapshot);
        Task CancelInstallation(string uniqueNodeId, ISnapshotHeader snapshotHeader);
        Task CompleteInstallation(string uniqueNodeId);
    }
}
