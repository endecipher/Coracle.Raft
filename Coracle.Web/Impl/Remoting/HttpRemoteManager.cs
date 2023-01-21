using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Samples.Logging;
using Coracle.Web.Controllers;

namespace Coracle.Web.Impl.Remoting
{
    public class HttpRemoteManager : IRemoteManager
    {
        public const string Entity = nameof(HttpRemoteManager);
        public const string Unsuccessful = nameof(Unsuccessful);
        public const string statusCode = nameof(statusCode);
        public const string stringContent = nameof(stringContent);

        public HttpRemoteManager(IHttpClientFactory httpClientFactory, IActivityLogger activityLogger)
        {
            HttpClientFactory = httpClientFactory;
            ActivityLogger = activityLogger;
        }

        internal IHttpClientFactory HttpClientFactory { get; set; }
        public IActivityLogger ActivityLogger { get; }

        public async Task<Operation<IAppendEntriesRPCResponse>> Send(IAppendEntriesRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            Operation<IAppendEntriesRPCResponse> operationResult = new Operation<IAppendEntriesRPCResponse>();

            try
            {
                var requestUri = new Uri(configuration.BaseUri, RaftController.AppendEntriesEndpoint);
                var httpClient = HttpClientFactory.CreateClient();

                var httpresponse = await httpClient.PostAsJsonAsync(requestUri, callObject, options: null, cancellationToken);

                if (httpresponse.IsSuccessStatusCode)
                {
                    var response = await httpresponse.Content.ReadFromJsonAsync<Operation<AppendEntriesRPCResponse>>(cancellationToken: cancellationToken);
                    operationResult.Exception = response.Exception;
                    operationResult.Response = response.Response;
                }
                else
                {
                    var code = httpresponse.StatusCode.ToString();
                    var content = httpresponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    ActivityLogger?.Log(new ImplActivity
                    {
                        EntitySubject = Entity,
                        Event = Unsuccessful,
                        Level = ActivityLogLevel.Error
                    }
                    .With(ActivityParam.New(statusCode, code))
                    .With(ActivityParam.New(stringContent, content)));

                    operationResult.Exception = new Exception(content);
                }

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
                var requestUri = new Uri(configuration.BaseUri, RaftController.RequestVoteEndpoint);
                var httpClient = HttpClientFactory.CreateClient();

                var httpresponse = await httpClient.PostAsJsonAsync(requestUri, callObject, options: null, cancellationToken);

                if (httpresponse.IsSuccessStatusCode)
                {
                    var response = await httpresponse.Content.ReadFromJsonAsync<Operation<RequestVoteRPCResponse>>(cancellationToken: cancellationToken);
                    operationResult.Exception = response.Exception;
                    operationResult.Response = response.Response;
                }
                else
                {
                    var code = httpresponse.StatusCode.ToString();
                    var content = httpresponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    ActivityLogger?.Log(new ImplActivity
                    {
                        EntitySubject = Entity,
                        Event = Unsuccessful,
                        Level = ActivityLogLevel.Error
                    }
                    .With(ActivityParam.New(statusCode, code))
                    .With(ActivityParam.New(stringContent, content)));

                    operationResult.Exception = new Exception(content);
                }
            }
            catch (Exception ex)
            {
                operationResult.Exception = ex;
            }

            return operationResult;
        }
    }
}
