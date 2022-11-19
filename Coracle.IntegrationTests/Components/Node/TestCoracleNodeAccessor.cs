using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Discovery;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Registries;
using Coracle.IntegrationTests.Components.Remoting;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Coracle.IntegrationTests.Components.Node
{
    public interface ICoracleNodeAccessor
    {
        ICanoeNode CoracleNode { get; }

        bool IsInitialized => CoracleNode != null;
    }

    public class TestCoracleNodeAccessor : ICoracleNodeAccessor
    {
        private object _lock = new object();
        private ICanoeNode _node = null;

        public ICanoeNode CoracleNode
        {

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
                    _node.InitializeConfiguration();
                    _node.Start();
                }
            }
        }

        /// <remarks>
        /// .NET DI Services and StructureMap jugalbandi
        /// </remarks>
        public TestCoracleNodeAccessor(IAppInfo appInfo, IEngineConfiguration engineConfig, IOptions<ContainerRegistryOptions> options, ICanoeNode node)
        {
            (engineConfig as EngineConfigurationSettings).ThisNodeUri = appInfo.GetCurrentAppUri();
            
            CoracleNode = node;
        }
    }
}
