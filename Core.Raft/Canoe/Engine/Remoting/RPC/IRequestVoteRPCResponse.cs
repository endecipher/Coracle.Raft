namespace Core.Raft.Canoe.Engine.Remoting.RPC
{
    public interface IRequestVoteRPCResponse : IRemoteResponse
    {
        long Term { get; }
        bool VoteGranted { get; }
    }
}