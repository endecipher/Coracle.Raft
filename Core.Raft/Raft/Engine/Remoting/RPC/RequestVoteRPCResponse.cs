using System;

namespace Coracle.Raft.Engine.Remoting.RPC
{
    [Serializable]
    public class RequestVoteRPCResponse : IRequestVoteRPCResponse
    {
        public long Term { get; init; }
        public bool VoteGranted { get; init; }
    }
}