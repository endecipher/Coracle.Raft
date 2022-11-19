using Coracle.Web.ClientHandling.NoteCommand;
using Coracle.Web.ClientHandling.Notes;
using Coracle.Web.Hubs;
using Coracle.Web.Logging;
using Coracle.Web.Node;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Coracle.Web.Controllers
{
    public class CommandController : Controller
    {
        public CommandController(INode node)
        {
            Node = node;
        }

        public INode Node { get; }

        [HttpGet]
        public string Index()
        {
            return "Ok";
        }

        [HttpPost(Name = nameof(AddNote))]
        public async Task<string> AddNote([FromBody] Note obj, [FromQuery] string tag)
        {
            var command = new AddNoteCommand(new Note
            {
                UniqueHeader = obj.UniqueHeader,
                Text = obj.Text
            });

            var result = await Node.CanoeNode.ExternalClientCommandHandler.HandleClientCommand(command, HttpContext.RequestAborted);

            return result.IsOperationSuccessful ? $"Added" : result.Exception.Message;
        }

        [HttpGet(Name = nameof(GetNote))]
        public async Task<string> GetNote([FromQuery] string UniqueHeader)
        {
            var command = new GetNoteCommand(UniqueHeader);

            var result = await Node.CanoeNode.ExternalClientCommandHandler.HandleClientCommand(command, HttpContext.RequestAborted);

            return result.CommandResult.ToString();
        }
    }
}
