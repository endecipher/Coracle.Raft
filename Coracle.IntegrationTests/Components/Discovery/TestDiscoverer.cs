using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Discovery.Registrar;

namespace Coracle.IntegrationTests.Components.Discovery
{
    public class TestDiscoverer : BaseDiscoverer
    {
        public TestDiscoverer(IActivityLogger activityLogger, 
            IEngineConfiguration engineConfiguration, 
            IClusterConfiguration clusterConfiguration,
            INodeRegistrar nodeRegistrar
            ) : base(activityLogger, engineConfiguration, clusterConfiguration)
        {
            NodeRegistrar = nodeRegistrar;
        }

        public IHttpClientFactory HttpClientFactory { get; set; }
        public INodeRegistrar NodeRegistrar { get; }

        protected override bool ThrowOnException => true;

        public override async Task<IDiscoveryOperation> EnrollThisNode(Uri registrarUri, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            return await NodeRegistrar.Enroll(configuration as NodeConfiguration, cancellationToken);

            //var client = HttpClientFactory.CreateClient();

            //var path = "discovery/enroll";

            //var enrollUri = new Uri(registrarUri, path);

            //var response = await client.PostAsJsonAsync(enrollUri, configuration, cancellationToken);

            //if (response.IsSuccessStatusCode)
            //{
            //    var re = await response.Content.ReadAsStringAsync(cancellationToken);
            //    return JsonConvert.DeserializeObject<DiscoveryOperation>(re);
            //}
            //else
            //{
            //    throw new InvalidOperationException();
            //}

            
        }

        public override async Task<IDiscoveryOperation> GetAllNodes(Uri registrarUri, CancellationToken cancellationToken)
        {
            return await NodeRegistrar.GetAllNodes(cancellationToken);

            //var client = HttpClientFactory.CreateClient();

            //var path = "discovery/get";

            //var getUri = new Uri(registrarUri, path);

            //var response = await client.GetAsync(getUri, cancellationToken);

            //if (response.IsSuccessStatusCode)
            //{
            //    var re = await response.Content.ReadAsStringAsync(cancellationToken);
            //    return JsonConvert.DeserializeObject<DiscoveryOperation>(re);
            //}
            //else
            //{
            //    throw new InvalidOperationException();
            //}
        }
    }
}
