using Coracle.IntegrationTests.Components.ClientHandling.Notes;

namespace Coracle.IntegrationTests.Components.ClientHandling.NoteCommand
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
