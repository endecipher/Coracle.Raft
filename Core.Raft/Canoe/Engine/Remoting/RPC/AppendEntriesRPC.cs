using Core.Raft.Canoe.Engine.Logs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Raft.Canoe.Engine.Remoting.RPC
{
    [Serializable]
    public class AppendEntriesRPC : IAppendEntriesRPC
    {
        public AppendEntriesRPC(IEnumerable<LogEntry> entries)
        {
            Entries = entries;
        }

        public bool IsHeartbeat => Entries == null || !Entries.Any();

        public long Term { get; init; }

        public string LeaderId { get; init; }

        public long PreviousLogIndex { get; init; }

        public long PreviousLogTerm { get; init; }

        public IEnumerable<LogEntry> Entries { get; private set; } = default;

        public long LeaderCommitIndex { get; init; }
    }
}