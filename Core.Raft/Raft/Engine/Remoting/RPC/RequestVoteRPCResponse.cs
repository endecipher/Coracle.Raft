namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IRequestVoteRPCResponse : IRemoteResponse
    {
        long Term { get; }
        bool VoteGranted { get; }
    }

    public class RequestVoteRPCResponse : IRequestVoteRPCResponse
    {
        public long Term { get; set; }
        public bool VoteGranted { get; set; }
    }
}