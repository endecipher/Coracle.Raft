using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Examples.Registrar;

namespace Coracle.Raft.Tests.Components.Discovery
{
    public class TestDiscoveryHandler : IDiscoveryHandler
    {
        public TestDiscoveryHandler(INodeRegistrar nodeRegistrar)
        {
            NodeRegistrar = nodeRegistrar;
        }

        public INodeRegistrar NodeRegistrar { get; }

        public async Task<DiscoveryResult> Enroll(INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            return await NodeRegistrar.Enroll(configuration as NodeConfiguration, cancellationToken);
        }

        public async Task<DiscoveryResult> GetAllNodes(CancellationToken cancellationToken)
        {
            return await NodeRegistrar.GetAllNodes(cancellationToken);
        }
    }
}
