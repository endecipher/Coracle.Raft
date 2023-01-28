using ActivityLogger.Logging;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Exceptions;
using Coracle.Samples.ClientHandling.NoteCommand;
using Coracle.Samples.Logging;
using Coracle.Web.Impl.Node;
using Newtonsoft.Json;

namespace Coracle.Web.Controllers
{
    public class CoracleClient : ICoracleClient
    {
        public const string Entity = nameof(CoracleClient);
        public const string Unsuccessful = nameof(Unsuccessful);
        public const string statusCode = nameof(statusCode);
        public const string stringContent = nameof(stringContent);

        public CoracleClient(
            IExternalClientCommandHandler externalClientCommandHandler, 
            IExternalConfigurationChangeHandler externalConfigurationChangeHandler,
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

        public IExternalClientCommandHandler ExternalClientCommandHandler { get; }
        public IExternalConfigurationChangeHandler ExternalConfigurationChangeHandler { get; }
        public ICoracleNodeAccessor NodeAccessor { get; }
        public IEngineConfiguration EngineConfiguration { get; }
        public IHttpClientFactory HttpClientFactory { get; }
        public IActivityLogger ActivityLogger { get; }

        public async Task<string> ExecuteCommand(NoteCommand command, CancellationToken token)
        {
            if (!NodeAccessor.CoracleNode.IsInitialized || !NodeAccessor.CoracleNode.IsStarted)
            {
                return "Node not started yet";
            }

            var result = await ExternalClientCommandHandler.HandleClientCommand(command, token);

            if (result.IsOperationSuccessful)
            {
                return $"Executed {command.UniqueId} {JsonConvert.SerializeObject(result.CommandResult) ?? string.Empty}";
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
                    string operationResult = string.Empty;

                    try
                    {
                        var requestUri = new Uri(result.LeaderNodeConfiguration.BaseUri, CommandController.ExternalHandlingEndpoint);
                        var httpClient = HttpClientFactory.CreateClient();

                        var httpresponse = await httpClient.PostAsJsonAsync(requestUri, command, options: null, token);

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
            else 
            {
                return result.Exception.Message;
            }
        }

        public async Task<string> ChangeConfiguration(ConfigurationChangeRPC changeRPC, CancellationToken token)
        {
            if (!NodeAccessor.CoracleNode.IsInitialized || !NodeAccessor.CoracleNode.IsStarted)
            {
                return "Node not started yet";
            }

            var result = await ExternalConfigurationChangeHandler.IssueConfigurationChange(changeRPC, token);

            if (result.IsOperationSuccessful)
            {
                return $"Configuration change initiated successfully. New Configuration: {JsonConvert.SerializeObject(changeRPC.NewConfiguration)}";
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
                    string operationResult = string.Empty;

                    try
                    {
                        var requestUri = new Uri(result.LeaderNodeConfiguration.BaseUri, ConfigurationChangeController.ExternalHandlingEndpoint);
                        var httpClient = HttpClientFactory.CreateClient();

                        var httpresponse = await httpClient.PostAsJsonAsync(requestUri, changeRPC, options: null, token);

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
            else
            {
                return result.Exception.Message;
            }
        }
    }
}
