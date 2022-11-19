namespace Rafty.Concensus.States
{
    using System.Threading.Tasks;
    using FiniteStateMachine;
    using Infrastructure;
    using Messages;

    public interface IState
    {
        CurrentState CurrentState { get; }
        Task<AppendEntriesResponse> Handle(AppendEntriesRPC appendEntries);
        Task<RequestVoteRPCResponse> Handle(RequestVote requestVote);
        Task<Response<T>> Accept<T>(T command) where T : ICommand;
        void Stop();
    }
}