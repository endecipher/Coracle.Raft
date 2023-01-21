using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Discovery;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Registries;
using Coracle.IntegrationTests.Components.Remoting;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
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
        public TestCoracleNodeAccessor(IAppInfo appInfo, IEngineConfiguration engineConfig, ICanoeNode node)
        {
            (engineConfig as EngineConfigurationSettings).ThisNodeUri = appInfo.GetCurrentAppUri();
            
            CoracleNode = node;
        }
    }
}
