using ActivityLogger.Logging;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Remoting;
using TaskGuidance.BackgroundProcessing.Dependencies;
using Coracle.Raft.Engine.Command;
using EntityMonitoring.FluentAssertions.Structure;
using Coracle.Raft.Examples.Registrar;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Examples.Data;
using Coracle.Raft.Tests.Components.Discovery;
using Coracle.Raft.Tests.Components.Logging;
using Coracle.Raft.Tests.Components.Remoting;

namespace Coracle.Raft.Tests.Framework
{
    public class TestRegistration : IDependencyRegistration
    {
        public void Register(IDependencyContainer container)
        {
            container.RegisterSingleton<IStateMachineHandler, NoteKeeperStateMachineHandler>();
            container.RegisterSingleton<IPersistentStateHandler, SampleVolatileStateHandler>();
            container.RegisterSingleton<IDiscoveryHandler, TestDiscoveryHandler>();
            container.RegisterSingleton<IOutboundRequestHandler, TestOutboundRequestHandler>();

            container.RegisterSingleton<INoteStorage, NoteStorage>();
            container.RegisterSingleton<IActivityLogger, TestActivityLogger>();
            container.RegisterSingleton<INodeRegistrar, TestNodeRegistrar>();
            container.RegisterSingleton<INodeRegistry, TestNodeRegistry>();
            container.RegisterSingleton<ISnapshotManager, SnapshotManager>();

            /// EntityMonitoring.FluentAssertions
            container.RegisterSingleton<IActivityMonitorSettings, ActivityMonitorSettings>();
            container.RegisterSingleton<IActivityMonitor<Activity>, ActivityMonitor<Activity>>();
        }
    }
}
