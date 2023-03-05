using Coracle.Raft.Engine.Logs;
using System.Collections.Generic;
using System.Linq;

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

    public class AppendEntriesRPC : IAppendEntriesRPC
    {
        public AppendEntriesRPC()
        {

        }

        public AppendEntriesRPC(IEnumerable<LogEntry> entries) : this()
        {
            Entries = entries;
        }

        public bool IsHeartbeat => Entries == null || !Entries.Any();

        public long Term { get; set; }

        public string LeaderId { get; set; }

        public long PreviousLogIndex { get; set; }

        public long PreviousLogTerm { get; set; }

        public IEnumerable<LogEntry> Entries { get; set; } = default;

        public long LeaderCommitIndex { get; set; }
    }
}