namespace Rafty.Concensus.Node
{
    using System.Threading.Tasks;
    using FiniteStateMachine;
    using Infrastructure;
    using Messages;
    using States;

    public interface INode
    {
        IState State { get; }
        void BecomeLeader(CurrentState state);
        void BecomeFollower(CurrentState state);
        void BecomeCandidate(CurrentState state);
        Task<AppendEntriesResponse> Handle(AppendEntriesRPC appendEntries);
        Task<RequestVoteRPCResponse> Handle(RequestVote requestVote);
        void Start(NodeId id);
        void Stop();
        Task<Response<T>> Accept<T>(T command) where T : ICommand;
    }
}