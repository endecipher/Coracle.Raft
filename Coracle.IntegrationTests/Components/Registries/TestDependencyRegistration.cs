using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Discovery;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Remoting;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.Discovery.Registrar;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Remoting;
using TaskGuidance.BackgroundProcessing.Dependencies;
using Coracle.Samples.ClientHandling.Notes;
using Coracle.Samples.ClientHandling;
using Coracle.Samples.PersistentData;

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
