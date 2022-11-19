using Raft.Web.Canoe.ClientHandling.Notes;

namespace Raft.Web.Canoe.ClientHandling.NoteCommand
{
    public class GetNoteCommand : BaseNoteCommand
    {
        public GetNoteCommand(string noteName) : base()
        {
            Data = noteName;
        }

        public override bool IsReadOnly => true;

        public override string Type => nameof(INotes.TryGet);
    }
}
