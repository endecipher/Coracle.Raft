using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Logs.CollectionStructure;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Extensions.Canoe.Remoting;
using EventGuidance.Logging;
using Raft.Web.Canoe.ClientHandling;
using Raft.Web.Canoe.ClientHandling.Notes;
using Raft.Web.Canoe.Configuration;
using Raft.Web.Canoe.Logging;
using Raft.Web.Canoe.PersistentData;
using StructureMap;

namespace Raft.Web.Canoe.Registries
{
    public class ExtensionRegistry : Registry
    {
        public IHttpClientFactory Factory { get; }

        public ExtensionRegistry(IHttpClientFactory factory)
        {
            Factory = factory;
        }

        public ExtensionRegistry()
        {
            For<INotes>().Use<Notes>().Singleton();
            For<IRemoteManager>().Use<HttpRemoteManager>().Singleton();
            For<IClientRequestHandler>().Use<SimpleClientRequestHandler>().Singleton();
            For<IEventLogger>().Use<SimpleEventLogger>().Singleton();
            For<IPersistentProperties>().Use<MemoryProperties>().Singleton();
            For<IPersistentReplicatedLogHolder>().Use<MemoryLogHolder> ().Singleton();
            For<IEventProcessorConfiguration>().Use<EventProcessorConfiguration>().Singleton();
            For<IClusterConfiguration>().Use<ClusterConfigurationSettings>().Singleton();
            For<ITimeConfiguration>().Use<TimeConfiguration>().Singleton();
            For<IOtherConfiguration>().Use<OtherConfiguration>().Singleton();
        }
    }
}
