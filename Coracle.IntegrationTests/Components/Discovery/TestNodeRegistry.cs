using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery.Registrar;
using System.Collections.Concurrent;

namespace Coracle.IntegrationTests.Components.Discovery
{
    public class TestNodeRegistry : INodeRegistry
    {
        ConcurrentDictionary<string, NodeConfiguration> MemoryRegistry = new ConcurrentDictionary<string, NodeConfiguration>();

        public Task AddOrUpdate(NodeConfiguration configuration)
        {
            MemoryRegistry.AddOrUpdate(configuration.UniqueNodeId, configuration, (key, conf) => configuration);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<NodeConfiguration>> GetAll()
        {
            return Task.FromResult((IEnumerable<NodeConfiguration>)MemoryRegistry.Values.AsEnumerable());
        }

        public Task TryRemove(string nodeId)
        {
            MemoryRegistry.TryRemove(nodeId, out var configuration);

            return Task.CompletedTask;
        }
    }
}
