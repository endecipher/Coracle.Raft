using Coracle.Raft.Engine.Configuration.Cluster;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Controllers
{
    public class ConfigurationChangeController : Controller
    {
        public const string ConfigurationChange = nameof(ConfigurationChange);
        public static string ExternalHandlingEndpoint => ConfigurationChange + "/" + nameof(HandleChange);

        public ConfigurationChangeController(ICoracleClient coracleClient)
        {
            CoracleClient = coracleClient;
        }

        public ICoracleClient CoracleClient { get; }

        [HttpGet]
        public string Index()
        {
            return nameof(ConfigurationChangeController);
        }

        [HttpPost(Name = nameof(HandleChange))]
        public async Task<string> HandleChange()
        {
            var command = await HttpContext.Request.ReadFromJsonAsync<ConfigurationChangeRPC>(HttpContext.RequestAborted);

            var result = await CoracleClient.ChangeConfiguration(command, HttpContext.RequestAborted);

            return result;
        }
    }
}
