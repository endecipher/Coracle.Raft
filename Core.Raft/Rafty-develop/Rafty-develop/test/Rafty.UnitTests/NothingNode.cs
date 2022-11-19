using System;
using Rafty.Concensus;
using Rafty.FiniteStateMachine;

namespace Rafty.UnitTests
{
    using System.Threading.Tasks;
    using Concensus.Messages;
    using Concensus.Node;
    using Concensus.States;
    using Infrastructure;

    public class NothingNode : INode
    {
        public IState State { get; }

        public int BecomeLeaderCount { get; private set; } 
        public int BecomeFollowerCount { get; private set; } 
        public int BecomeCandidateCount { get; private set; }

        public void BecomeLeader(CurrentState state)
        {
            BecomeLeaderCount++;
        }

        public void BecomeFollower(CurrentState state)
        {
            BecomeFollowerCount++;
        }

        public void BecomeCandidate(CurrentState state)
        {
            BecomeCandidateCount++;
        }

        public async Task<AppendEntriesResponse> Handle(AppendEntriesRPC appendEntries)
        {
            return new AppendEntriesResponseBuilder().Build();
        }

        public async Task<RequestVoteRPCResponse> Handle(RequestVote requestVote)
        {
            return new RequestVoteResponseBuilder().Build();
        }

        public void Start(NodeId id)
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<T>> Accept<T>(T command) where T : ICommand
        {
            throw new System.NotImplementedException();
        }
    }
}