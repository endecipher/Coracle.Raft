using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Newtonsoft.Json;

namespace Coracle.Web.Coracle.Discovery
{
    public class HttpDiscoverer : BaseDiscoverer
    {
        public HttpDiscoverer(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        public IHttpClientFactory HttpClientFactory { get; set; }

        protected override bool ThrowOnException => true;

        public override async Task<IDiscoveryOperation> EnrollThisNode(Uri registrarUri, INodeConfiguration configuration, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.CreateClient();

            var path = "discovery/enroll";

            var enrollUri = new Uri(registrarUri, path);

            var response = await client.PostAsJsonAsync(enrollUri, configuration, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var re = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonConvert.DeserializeObject<DiscoveryOperation>(re);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override async Task<IDiscoveryOperation> GetAllNodes(Uri registrarUri, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.CreateClient();

            var path = "discovery/get";

            var getUri = new Uri(registrarUri, path);

            var response = await client.GetAsync(getUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var re = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonConvert.DeserializeObject<DiscoveryOperation>(re);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
