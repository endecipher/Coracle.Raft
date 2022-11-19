using System;
using Rafty.Concensus;
using Rafty.FiniteStateMachine;

namespace Rafty.UnitTests
{
    using System.Threading.Tasks;
    using Concensus.Messages;
    using Concensus.Peers;
    using Infrastructure;

    public class RemoteControledPeer : IPeer
    {
        private RequestVoteRPCResponse _requestVoteResponse;
        private AppendEntriesResponse _appendEntriesResponse;
        public int RequestVoteResponses { get; private set; }
        public int AppendEntriesResponses { get; private set; }
        public int AppendEntriesResponsesWithLogEntries {get;private set;}

        public RemoteControledPeer()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; }

        public void SetRequestVoteResponse(RequestVoteRPCResponse requestVoteResponse)
        {
            _requestVoteResponse = requestVoteResponse;
        }

        public void SetAppendEntriesResponse(AppendEntriesResponse appendEntriesResponse)
        {
            _appendEntriesResponse = appendEntriesResponse;
        }

        public async Task<RequestVoteRPCResponse> Request(RequestVote requestVote)
        {
            RequestVoteResponses++;
            return _requestVoteResponse;
        }

        public async Task<AppendEntriesResponse> Request(AppendEntriesRPC appendEntries)
        {
            if(appendEntries.Entries.Count > 0)
            {
                AppendEntriesResponsesWithLogEntries++;
            }
            AppendEntriesResponses++;
            return _appendEntriesResponse;
        }

        public async Task<Response<T>> Request<T>(T command) where T : ICommand
        {
            throw new NotImplementedException();
        }
    }
}