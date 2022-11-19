using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Logs.CollectionStructure;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Logging;
using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Discovery;
using Coracle.IntegrationTests.Components.Discovery;
using Coracle.IntegrationTests.Components.ClientHandling.Notes;
using Coracle.IntegrationTests.Components.ClientHandling;
using Coracle.IntegrationTests.Components.PersistentData;
using Coracle.IntegrationTests.Components.Logging;
using EventGuidance.Dependency;
using Coracle.IntegrationTests.Components.Remoting;
using Core.Raft.Canoe.Engine.Discovery.Registrar;

namespace Coracle.IntegrationTests.Components.Registries
{
    public class TestDependencyRegistration : IDependencyRegistration
    {
        public void Register(IDependencyContainer container)
        {
            container.RegisterSingleton<INotes, Notes>();
            container.RegisterSingleton<IClientRequestHandler, TestClientRequestHandler>();
            container.RegisterSingleton<IPersistentProperties, TestStateProperties>();
            container.RegisterSingleton<IDiscoverer, TestDiscoverer>();
            container.RegisterSingleton<IRemoteManager, TestRemoteManager>();
            container.RegisterSingleton<IActivityLogger, TestActivityLogger>();
            container.RegisterSingleton<INodeRegistrar, TestNodeRegistrar>();
            container.RegisterSingleton<INodeRegistry, TestNodeRegistry>();
        }
    }
}
