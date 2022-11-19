using ActivityLogger.Logging;
using Coracle.Web.Configuration;
using Coracle.Web.Coracle.Discovery;
using Coracle.Web.Coracle.Remoting;
using Coracle.Web.Hubs;
using Coracle.Web.Logging;
using Coracle.Web.Registries;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting;
using CorrelationId.Abstractions;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Coracle.Web.Node
{
    public interface INode
    {
        ICanoeNode CanoeNode { get; }

        bool IsInitialized => CanoeNode != null;
    }

    public class CanoeNodeAccessor : INode
    {
        private object _lock =  new object();
        private ICanoeNode _node = null;
        private readonly IEngineConfiguration _engineConfig = null;

        public ICanoeNode CanoeNode { 

            get
            {
                lock (_lock)
                {
                    return _node;
                }
            }

            private set
            {
                lock (_lock)
                {
                    _node = value;
                    _node.InitializeConfiguration(_engineConfig);
                    _node.Start();
                }
            }
        }

        /// <remarks>
        /// .NET DI Services and StructureMap jugalbandi
        /// </remarks>
        public CanoeNodeAccessor(IAppInfo appInfo, IOptions<EngineConfigurationSettings> engineConfig, IOptions<ContainerRegistryOptions> options)
        {
            //InjectSignalRInActivityLogger(configuration, hubContext);
            //InjectHttpClientFactory(httpClientFactory);


            engineConfig.Value.ThisNodeUri = appInfo.GetCurrentAppUri();

            _engineConfig = engineConfig.Value;

            if (options.Value.ConfigureHandler)
            {
                options.Value.HandlerAction.Invoke(ComponentContainer.Instance);
            }

            CanoeNode = ComponentContainer.Instance.GetInstance<ICanoeNode>();
        }

        private static void InjectHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            (ComponentContainer.Instance.GetInstance<IDiscoverer>() as HttpDiscoverer).HttpClientFactory = httpClientFactory;
            (ComponentContainer.Instance.GetInstance<IRemoteManager>() as HttpRemoteManager).HttpClientFactory = httpClientFactory;
        }

        private static void InjectSignalRInActivityLogger(IConfiguration configuration, IHubContext<LogHub> hubContext)
        {
            var logLevel = configuration.GetSection("Coracle:LogLevel").Get<string>();

            if (Enum.TryParse<ActivityLogger.Logging.ActivityLogLevel>(logLevel, out var val))
            {
                var webLogger = (ComponentContainer.Instance.GetInstance<IActivityLogger>() as ActivityLoggerImpl);

                webLogger.Level = val;
                //webLogger.LogHubContext = hubContext;
            }
        }
    }
}
