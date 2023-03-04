namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IRequestVoteRPC : IRemoteCall
    {
        string CandidateId { get; }
        long LastLogIndex { get; }
        long LastLogTerm { get; }
        long Term { get; }
    }

    public class RequestVoteRPC : IRequestVoteRPC
    {
        public long Term { get; set; }
        public string CandidateId { get; set; }
        public long LastLogIndex { get; set; }
        public long LastLogTerm { get; set; }
    }
}