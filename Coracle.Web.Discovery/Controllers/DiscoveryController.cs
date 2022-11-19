using Coracle.Web.Discovery.Coracle.Registrar;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Discovery.Registrar;
using Microsoft.AspNetCore.Http.Extensions;
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
        public async Task<IDiscoveryOperation> Enroll()
        {
            var obj = await HttpContext.Request.ReadFromJsonAsync<NodeConfiguration>(HttpContext.RequestAborted);

            return await NodeRegistrar.Enroll(obj, HttpContext.RequestAborted);
        }

        [HttpGet(Name = nameof(Get))]
        public async Task<IDiscoveryOperation> Get()
        {
            return await NodeRegistrar.GetAllNodes(HttpContext.RequestAborted);
        }
    }
}
