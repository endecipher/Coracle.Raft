using Coracle.Samples.ClientHandling.Notes;

namespace Coracle.Samples.ClientHandling.NoteCommand
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
