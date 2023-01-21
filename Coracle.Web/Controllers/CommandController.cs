using Coracle.Samples.ClientHandling.NoteCommand;
using Coracle.Samples.ClientHandling.Notes;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Controllers
{
    public class CommandController : Controller
    {
        public const string Command = nameof(Command);
        public const string AddNoteConstant = nameof(AddNote);
        public const string GetNoteConstant = nameof(GetNote);

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
            var command = new AddNoteCommand(new Note
            {
                UniqueHeader = obj.UniqueHeader,
                Text = obj.Text
            });

            return await CoracleClient.ExecuteCommand(command, HttpContext.RequestAborted);
        }

        [HttpGet(Name = nameof(GetNote))]
        public async Task<string> GetNote([FromQuery] string noteHeader)
        {
            var command = new GetNoteCommand(noteHeader);

            return await CoracleClient.ExecuteCommand(command, HttpContext.RequestAborted);
        }
    }
}
