using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.Discovery.Registrar;

namespace Coracle.IntegrationTests.Components.Discovery
{
    public class TestDiscoverer : IDiscoverer
    {
        public TestDiscoverer(INodeRegistrar nodeRegistrar) 
        {
            NodeRegistrar = nodeRegistrar;
        }

        public INodeRegistrar NodeRegistrar { get; }

        public async Task<IDiscoveryOperation> EnrollThisNode(Uri registrarUri, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            return await NodeRegistrar.Enroll(configuration as NodeConfiguration, cancellationToken);
        }

        public async Task<IDiscoveryOperation> GetAllNodes(Uri registrarUri, CancellationToken cancellationToken)
        {
            return await NodeRegistrar.GetAllNodes(cancellationToken);
        }
    }
}
