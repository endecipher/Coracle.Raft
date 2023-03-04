using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Coracle.Web.Impl.Discovery
{
    public class HttpDiscoveryHandler : IDiscoveryHandler
    {
        public HttpDiscoveryHandler(IHttpClientFactory httpClientFactory, IOptions<EngineConfigurationOptions> engineConfigurationOptions)
        {
            HttpClientFactory = httpClientFactory;
            EngineConfigurationOptions = engineConfigurationOptions;
        }

        public IHttpClientFactory HttpClientFactory { get; set; }
        public IOptions<EngineConfigurationOptions> EngineConfigurationOptions { get; }

        public async Task<DiscoveryResult> Enroll(INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.CreateClient();

            var enrollUri = new Uri(EngineConfigurationOptions.Value.DiscoveryServerUri, Constants.Discovery.Enroll);

            var response = await client.PostAsJsonAsync(enrollUri, configuration, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var contentString = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonConvert.DeserializeObject<DiscoveryResult>(contentString);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public async Task<DiscoveryResult> GetAllNodes(CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.CreateClient();

            var getUri = new Uri(EngineConfigurationOptions.Value.DiscoveryServerUri, Constants.Discovery.GetCluster);

            var response = await client.GetAsync(getUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var contentString = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonConvert.DeserializeObject<DiscoveryResult>(contentString);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
