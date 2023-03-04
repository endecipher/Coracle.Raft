using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Web.Client;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Controllers
{
    public class ConfigurationChangeController : Controller
    {
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
            var command = await HttpContext.Request.ReadFromJsonAsync<ConfigurationChangeRequest>(HttpContext.RequestAborted);

            var result = await CoracleClient.ChangeConfiguration(command, HttpContext.RequestAborted);

            return result;
        }
    }
}
