using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Discovery;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Remoting;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Remoting;
using TaskGuidance.BackgroundProcessing.Dependencies;
using Coracle.Raft.Engine.Command;
using EntityMonitoring.FluentAssertions.Structure;

namespace Coracle.IntegrationTests.Framework
{
    public class TestRegistration : IDependencyRegistration
    {
        public void Register(IDependencyContainer container)
        {
            container.RegisterSingleton<IStateMachineHandler, Samples.ClientHandling.NoteKeeperStateMachineHandler>();
            container.RegisterSingleton<IPersistentStateHandler, Samples.Data.SampleVolatileStateHandler>();
            container.RegisterSingleton<IDiscoveryHandler, TestDiscoveryHandler>();
            container.RegisterSingleton<IOutboundRequestHandler, TestOutboundRequestHandler>();

            container.RegisterSingleton<Samples.ClientHandling.INoteStorage, Samples.ClientHandling.NoteStorage>();
            container.RegisterSingleton<IActivityLogger, TestActivityLogger>();
            container.RegisterSingleton<Samples.Registrar.INodeRegistrar, TestNodeRegistrar>();
            container.RegisterSingleton<Samples.Registrar.INodeRegistry, TestNodeRegistry>();
            container.RegisterSingleton<Samples.Data.ISnapshotManager, Samples.Data.SnapshotManager>();

            /// EntityMonitoring.FluentAssertions
            container.RegisterSingleton<IActivityMonitorSettings, ActivityMonitorSettings>();
            container.RegisterSingleton<IActivityMonitor<Activity>, ActivityMonitor<Activity>>();
        }
    }
}
