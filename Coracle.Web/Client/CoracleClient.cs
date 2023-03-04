using ActivityLogger.Logging;
using Coracle.Raft.Engine.Command;
using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Exceptions;
using Coracle.Raft.Engine.Node;
using Coracle.Samples.ClientHandling;
using Coracle.Samples.Logging;
using Coracle.Web.Impl.Node;
using Newtonsoft.Json;
using static Coracle.Web.Constants;

namespace Coracle.Web.Client
{
    public class CoracleClient : ICoracleClient
    {
        public const string Entity = nameof(CoracleClient);
        public const string Unsuccessful = nameof(Unsuccessful);
        public const string statusCode = nameof(statusCode);
        public const string stringContent = nameof(stringContent);

        public CoracleClient(
            ICommandExecutor externalClientCommandHandler,
            IConfigurationRequestExecutor externalConfigurationChangeHandler,
            ICoracleNodeAccessor nodeAccessor,
            IEngineConfiguration engineConfiguration,
            IHttpClientFactory httpClientFactory,
            IActivityLogger activityLogger)
        {
            ExternalClientCommandHandler = externalClientCommandHandler;
            ExternalConfigurationChangeHandler = externalConfigurationChangeHandler;
            NodeAccessor = nodeAccessor;
            EngineConfiguration = engineConfiguration;
            HttpClientFactory = httpClientFactory;
            ActivityLogger = activityLogger;
        }

        public ICommandExecutor ExternalClientCommandHandler { get; }
        public IConfigurationRequestExecutor ExternalConfigurationChangeHandler { get; }
        public ICoracleNodeAccessor NodeAccessor { get; }
        public IEngineConfiguration EngineConfiguration { get; }
        public IHttpClientFactory HttpClientFactory { get; }
        public IActivityLogger ActivityLogger { get; }

        public async Task<string> ExecuteCommand(NoteCommand command, CancellationToken token)
        {
            if (!NodeAccessor.CoracleNode.IsInitialized || !NodeAccessor.CoracleNode.IsStarted)
            {
                return Strings.Errors.NodeNotReady;
            }

            var result = await ExternalClientCommandHandler.Execute(command, token);

            if (result.IsSuccessful)
            {
                return $"{Strings.Actions.Command}{Strings.Actions.Successful}{command.UniqueId} {JsonConvert.SerializeObject(result.CommandResult) ?? string.Empty}";
            }
            else if (result.Exception != null && result.Exception is ClientCommandDeniedException)
            {
                if (result.LeaderNodeConfiguration == null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(EngineConfiguration.NoLeaderElectedWaitInterval_InMilliseconds));
                    return await ExecuteCommand(command, token);
                }
                else
                {
                    return await ForwardToLeader(Routes.CommandEndpoint, result.LeaderNodeConfiguration.BaseUri, command, token);
                }
            }
            else
            {
                return result.Exception.Message;
            }
        }

        public async Task<string> ChangeConfiguration(ConfigurationChangeRequest changeRPC, CancellationToken token)
        {
            if (!NodeAccessor.CoracleNode.IsInitialized || !NodeAccessor.CoracleNode.IsStarted)
            {
                return Strings.Errors.NodeNotReady;
            }

            var result = await ExternalConfigurationChangeHandler.IssueChange(changeRPC, token);

            if (result.IsSuccessful)
            {
                return $"{Strings.Actions.ConfigurationChange}{Strings.Actions.Successful}{JsonConvert.SerializeObject(changeRPC.NewConfiguration)}";
            }
            else if (result.Exception != null && result.Exception is ConfigurationChangeDeniedException)
            {
                if (result.LeaderNodeConfiguration == null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(EngineConfiguration.NoLeaderElectedWaitInterval_InMilliseconds));
                    return await ChangeConfiguration(changeRPC, token);
                }
                else
                {
                    return await ForwardToLeader(Routes.ConfigurationChangeEndpoint, result.LeaderNodeConfiguration.BaseUri, changeRPC, token);
                }
            }
            else
            {
                return result.Exception.Message;
            }
        }

        private async Task<string> ForwardToLeader(string path, Uri leaderUri, object rpc, CancellationToken token)
        {
            string operationResult;

            try
            {
                var requestUri = new Uri(leaderUri, path);
                var httpClient = HttpClientFactory.CreateClient();

                var httpresponse = await httpClient.PostAsJsonAsync(requestUri, rpc, options: null, token);

                if (httpresponse.IsSuccessStatusCode)
                {
                    operationResult = await httpresponse.Content.ReadAsStringAsync(cancellationToken: token);

                    return operationResult;
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

                    operationResult = $"Error encountered. StatusCode: {code} Message: {content}";
                }

            }
            catch (Exception ex)
            {
                operationResult = ex.ToString();
            }

            return operationResult;
        }
    }
}
