using System;

namespace Core.Raft.Canoe.Engine.Remoting.RPC
{
    [Serializable]
    public class RequestVoteRPC : IRequestVoteRPC
    {
        public long Term { get; init; }
        public string CandidateId { get; init; }
        public long LastLogIndex { get; init; }
        public long LastLogTerm { get; init; }
    }
}