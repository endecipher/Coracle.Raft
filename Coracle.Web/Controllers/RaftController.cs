using Coracle.Web.Node;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Controllers
{
    public class RaftController : Controller
    {
        public RaftController(INode node)
        {
            Node = node;
        }

        public INode Node { get; }

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

        [HttpPost(Name = Routes.Endpoints.RefreshDiscovery)]
        public StatusCodeResult RefreshDiscovery()
        {
            Node.CanoeNode.Discoverer.RefreshDiscovery().Start();
            return StatusCode(200);
        }


        public static async Task<TContent> GetObject<TContent>(HttpRequest httpRequest)
        {
            return await httpRequest.ReadFromJsonAsync<TContent>();
        }
    }

}
