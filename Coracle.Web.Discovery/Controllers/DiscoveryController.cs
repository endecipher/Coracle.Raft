using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Examples.Registrar;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Discovery.Controllers
{
    public class DiscoveryController : Controller
    {
        public DiscoveryController(INodeRegistrar nodeRegistrar)
        {
            NodeRegistrar = nodeRegistrar;
        }

        public INodeRegistrar NodeRegistrar { get; }

        [HttpPost(Name = nameof(Enroll))]
        public async Task<DiscoveryResult> Enroll()
        {
            var obj = await HttpContext.Request.ReadFromJsonAsync<NodeConfiguration>(HttpContext.RequestAborted);

            return await NodeRegistrar.Enroll(obj, HttpContext.RequestAborted);
        }

        [HttpGet(Name = nameof(Get))]
        public async Task<DiscoveryResult> Get()
        {
            return await NodeRegistrar.GetAllNodes(HttpContext.RequestAborted);
        }

        [HttpGet(Name = nameof(Clear))]
        public async Task<DiscoveryResult> Clear()
        {
            await NodeRegistrar.Clear();
            
            return new DiscoveryResult
            {
                IsSuccessful = true,
            };
        }
    }
}
