using System;

namespace Core.Raft.Canoe.Engine.Remoting.RPC
{
    [Serializable]
    public class RequestVoteRPCResponse : IRequestVoteRPCResponse
    {
        public long Term { get; init; }
        public bool VoteGranted { get; init; }
    }
}