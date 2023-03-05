using Coracle.Raft.Examples.ClientHandling;
using Coracle.Web.Client;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Controllers
{
    public class CommandController : Controller
    {

        public CommandController(ICoracleClient coracleClient)
        {
            CoracleClient = coracleClient;
        }

        public ICoracleClient CoracleClient { get; }

        [HttpGet]
        public string Index()
        {
            return nameof(CommandController);
        }

        [HttpPost(Name = nameof(AddNote))]
        public async Task<string> AddNote([FromBody] Note obj, [FromQuery] string tag)
        {
            var command = NoteCommand.CreateAdd(obj);

            var result = await CoracleClient.ExecuteCommand(command, HttpContext.RequestAborted);

            return result;
        }

        [HttpGet(Name = nameof(GetNote))]
        public async Task<string> GetNote([FromQuery] string noteHeader)
        {
            var command = NoteCommand.CreateGet(new Note
            {
                UniqueHeader = noteHeader,
            });

            var result = await CoracleClient.ExecuteCommand(command, HttpContext.RequestAborted);

            return result;
        }

        [HttpPost(Name = nameof(HandleCommand))]
        public async Task<string> HandleCommand()
        {
            var command = await HttpContext.Request.ReadFromJsonAsync<NoteCommand>(HttpContext.RequestAborted);

            var result = await CoracleClient.ExecuteCommand(command, HttpContext.RequestAborted);

            return result;
        }
    }
}
