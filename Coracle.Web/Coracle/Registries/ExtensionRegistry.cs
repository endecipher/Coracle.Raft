using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Logs.CollectionStructure;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Logging;
using Coracle.Web.ClientHandling;
using Coracle.Web.ClientHandling.Notes;
using Coracle.Web.Configuration;
using Coracle.Web.Logging;
using Coracle.Web.PersistentData;
using StructureMap;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using CorrelationId;
using Coracle.Web.Coracle.Logging;
using Core.Raft.Canoe.Engine.Discovery;
using Coracle.Web.Coracle.Discovery;
using Coracle.Web.Coracle.Remoting;

namespace Coracle.Web.Registries
{
    public class ExtensionRegistry : Registry
    {
        public ExtensionRegistry()
        {
            For<INotes>().Use<Notes>().Singleton();
            //For<IRemoteManager>().Use<HttpRemoteManager>().Singleton();
            For<IClientRequestHandler>().Use<SimpleClientRequestHandler>().Singleton();
            For<IPersistentProperties>().Use<MemoryProperties>().Singleton();
            For<IPersistentReplicatedLogHolder>().Use<MemoryLogHolder> ().Singleton();
            For<IEventProcessorConfiguration>().Use<EventProcessorConfiguration>().Singleton();
            //For<IEngineConfiguration>().Use<TimeConfiguration>().Singleton();
            //For<IEngineConfiguration>().Use<OtherConfiguration>().Singleton();
            For<IActivityLogger>().Use<ActivityLoggerImpl>().Singleton();
            For<IActivityContext>().Use<ImplActivityContext>().Transient();
            For<ICorrelationContextAccessor>().Use<CorrelationContextAccessor>().Transient();
            For<IDiscoverer>().Use<HttpDiscoverer>().Singleton();
        }
    }
}
