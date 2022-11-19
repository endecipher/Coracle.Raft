using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Raft.Web.Canoe.Logging;
using Raft.Web.Canoe.Node;
using Raft.Web.Hubs;

namespace Raft.Web.Controllers
{
    [ApiController]
    [Route(Routes.Coracle)]
    public class CoracleController : ControllerBase
    {
        public CoracleController(INode node)
        {
            Node = node;
        }

        public INode Node { get; }

        [HttpGet(Name = Routes.Endpoints.Initialize)]
        public IActionResult Initialize([FromServices] IHubContext<LogHub> hubContext)
        {
            // Link .NET Singletons
            (ComponentContainer.Instance.GetInstance<IEventLogger>() as SimpleEventLogger).LogHubContext = hubContext;

            try
            {
                Node.Initialize();
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex.Message);
            }

            return Ok();
        }

        [HttpPost(Name = Routes.Endpoints.AppendEntries)]
        public async Task<Operation<IAppendEntriesRPCResponse>> AppendEntries()
        {
            var rpc = await GetObject<IAppendEntriesRPC>(HttpContext.Request);
            return await Node.CanoeNode.ExternalRpcHandler.RespondTo(rpc, HttpContext.RequestAborted);
        }

        [HttpPost(Name = Routes.Endpoints.RequestVote)]
        public async Task<Operation<IRequestVoteRPCResponse>> RequestVote()
        {
            var rpc = await GetObject<IRequestVoteRPC>(HttpContext.Request);
            return await Node.CanoeNode.ExternalRpcHandler.RespondTo(rpc, HttpContext.RequestAborted);
        }

        [HttpPost(Name = Routes.Endpoints.HandleClientCommand)]
        public async Task<Operation<IAppendEntriesRPCResponse>> HandleClientCommand()
        {
            var rpc = await GetObject<IAppendEntriesRPC>(HttpContext.Request);
            return await Node.CanoeNode.ExternalRpcHandler.RespondTo(rpc, HttpContext.RequestAborted);
        }

        public static async Task<TContent> GetObject<TContent>(HttpRequest httpRequest)
        {
            return await httpRequest.ReadFromJsonAsync<TContent>();
        }
    }
}