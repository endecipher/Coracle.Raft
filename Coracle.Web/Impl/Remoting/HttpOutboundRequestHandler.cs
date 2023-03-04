using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Samples.Logging;
using Coracle.Web.Controllers;

namespace Coracle.Web.Impl.Remoting
{
    public class HttpOutboundRequestHandler : IOutboundRequestHandler
    {
        public const string Entity = nameof(HttpOutboundRequestHandler);
        public const string Unsuccessful = nameof(Unsuccessful);
        public const string statusCode = nameof(statusCode);
        public const string stringContent = nameof(stringContent);

        public HttpOutboundRequestHandler(IHttpClientFactory httpClientFactory, IActivityLogger activityLogger)
        {
            HttpClientFactory = httpClientFactory;
            ActivityLogger = activityLogger;
        }

        IHttpClientFactory HttpClientFactory { get; set; }
        IActivityLogger ActivityLogger { get; }


        public async Task<RemoteCallResult<IAppendEntriesRPCResponse>> Send(IAppendEntriesRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            var requestUri = new Uri(configuration.BaseUri, Constants.Routes.AppendEntriesEndpoint);

            var (response, exception) = await Handle<IAppendEntriesRPC, AppendEntriesRPCResponse>(requestUri, callObject, configuration, cancellationToken);

            return new RemoteCallResult<IAppendEntriesRPCResponse>
            {
                Response = response,
                Exception = exception
            };
        }

        public async Task<RemoteCallResult<IRequestVoteRPCResponse>> Send(IRequestVoteRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            var requestUri = new Uri(configuration.BaseUri, Constants.Routes.RequestVoteEndpoint);

            var (response, exception) = await Handle<IRequestVoteRPC, RequestVoteRPCResponse>(requestUri, callObject, configuration, cancellationToken);

            return new RemoteCallResult<IRequestVoteRPCResponse>
            {
                Response = response,
                Exception = exception
            };
        }

        public async Task<RemoteCallResult<IInstallSnapshotRPCResponse>> Send(IInstallSnapshotRPC callObject, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            var requestUri = new Uri(configuration.BaseUri, Constants.Routes.InstallSnapshotEndpoint);

            var (response, exception) = await Handle<IInstallSnapshotRPC, InstallSnapshotRPCResponse>(requestUri, callObject, configuration, cancellationToken);

            return new RemoteCallResult<IInstallSnapshotRPCResponse>
            {
                Response = response,
                Exception = exception
            };
        }

        private async Task<(TResponse Response, Exception ex)> Handle<TRequest, TResponse>(Uri requestUri, TRequest callObject, INodeConfiguration configuration, CancellationToken cancellationToken) where TResponse: IRemoteResponse
        {
            TResponse responseObject = default(TResponse);
            Exception exception = null;

            try
            {
                var httpClient = HttpClientFactory.CreateClient();

                var httpresponse = await httpClient.PostAsJsonAsync(requestUri, callObject, options: null, cancellationToken);

                if (httpresponse.IsSuccessStatusCode)
                {
                    var response = await httpresponse.Content.ReadFromJsonAsync<RemoteCallResult<TResponse>>(cancellationToken: cancellationToken);

                    exception = response.Exception;
                    responseObject = response.Response;
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

                    exception = new Exception(content);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return (responseObject, exception);
        }
    }
}
