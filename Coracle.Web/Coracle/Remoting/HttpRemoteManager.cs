using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.Remoting.RPC;

namespace Coracle.Web.Coracle.Remoting
{
    public class HttpRemoteManager : IRemoteManager
    {
        internal IHttpClientFactory HttpClientFactory { get; set; }

        public async Task<Operation<IAppendEntriesRPCResponse>> Send(IAppendEntriesRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            Operation<IAppendEntriesRPCResponse> operationResult = new Operation<IAppendEntriesRPCResponse>();

            try
            {
                var requestUri = new Uri(configuration.BaseUri, "AppendEntries");
                var httpClient = HttpClientFactory.CreateClient();

                var httpresponse = await httpClient.PostAsJsonAsync(requestUri, callObject, options: null, cancellationToken);

                var appendEntriesRPCResponse = await httpresponse.Content.ReadFromJsonAsync<IAppendEntriesRPCResponse>(cancellationToken: cancellationToken);
                operationResult.Response = appendEntriesRPCResponse;
            }
            catch (Exception ex)
            {
                operationResult.Exception = ex;
            }

            return operationResult;
        }

        public async Task<Operation<IRequestVoteRPCResponse>> Send(IRequestVoteRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            Operation<IRequestVoteRPCResponse> operationResult = new Operation<IRequestVoteRPCResponse>();

            try
            {
                var requestUri = new Uri(configuration.BaseUri, "RequestVote");
                var httpClient = HttpClientFactory.CreateClient();

                var httpresponse = await httpClient.PostAsJsonAsync(requestUri, callObject, options: null, cancellationToken);

                var requestVoteRPCResponse = await httpresponse.Content.ReadFromJsonAsync<IRequestVoteRPCResponse>(cancellationToken: cancellationToken);
                operationResult.Response = requestVoteRPCResponse;
            }
            catch (Exception ex)
            {
                operationResult.Exception = ex;
            }

            return operationResult;
        }

        public async Task<ClientHandlingResult> ForwardToLeader<TCommand>(TCommand command, INodeConfiguration configuration, CancellationToken cancellationToken)
            where TCommand : class, ICommand
        {
            ClientHandlingResult operationResult = new ClientHandlingResult();

            try
            {
                var requestUri = new Uri(configuration.BaseUri, "CommandHandling");
                var httpClient = HttpClientFactory.CreateClient();

                var httpresponse = await httpClient.PostAsJsonAsync(requestUri, command, options: null, cancellationToken);

                operationResult = await httpresponse.Content.ReadFromJsonAsync<ClientHandlingResult>(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                operationResult.Exception = ex;
            }

            return operationResult;
        }
    }
}
