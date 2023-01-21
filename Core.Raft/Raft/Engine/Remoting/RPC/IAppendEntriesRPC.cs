using Coracle.Raft.Engine.Logs;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Remoting.RPC
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