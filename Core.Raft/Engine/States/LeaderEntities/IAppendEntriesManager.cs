using Coracle.Raft.Engine.Snapshots;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States.LeaderEntities
{
    public interface IAppendEntriesManager
    {
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
