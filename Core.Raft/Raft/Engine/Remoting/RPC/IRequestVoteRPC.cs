﻿namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IRequestVoteRPC : IRemoteCall
    {
        string CandidateId { get; }
        long LastLogIndex { get; }
        long LastLogTerm { get; }
        long Term { get; }
    }
}