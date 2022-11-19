using Coracle.IntegrationTests.Components.ClientHandling.Notes;

namespace Coracle.IntegrationTests.Components.ClientHandling.NoteCommand
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
