using Core.Raft.Canoe.Engine.Logs;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Remoting.RPC
{
    public interface IAppendEntriesRPC : IRemoteCall
    {
        IEnumerable<LogEntry> Entries { get; }
        bool IsHeartbeat { get; }
        long LeaderCommitIndex { get; }
        string LeaderId { get; }
        long PreviousLogIndex { get; }
        long PreviousLogTerm { get; }
        long Term { get; }
    }
}