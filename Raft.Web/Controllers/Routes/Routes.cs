namespace Raft.Web.Controllers
{
    public class Routes
    {
        public const string Coracle = nameof(Coracle);
        public class Endpoints
        {
            public const string AppendEntries = nameof(AppendEntries);
            public const string RequestVote = nameof(RequestVote);
            public const string HandleClientCommand = nameof(HandleClientCommand);
            public const string Initialize = nameof(Initialize);
        }
    }
}
