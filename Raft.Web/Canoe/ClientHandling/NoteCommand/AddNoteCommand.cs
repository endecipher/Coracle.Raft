using Raft.Web.Canoe.ClientHandling.Notes;

namespace Raft.Web.Canoe.ClientHandling.NoteCommand
{
    public class AddNoteCommand : BaseNoteCommand
    {
        public AddNoteCommand(Note note) : base()
        {
            Data = note;
        }

        public override bool IsReadOnly => false;

        public override string Type => nameof(INotes.Add);
    }
}
