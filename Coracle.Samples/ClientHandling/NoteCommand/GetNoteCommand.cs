using Coracle.Samples.ClientHandling.Notes;

namespace Coracle.Samples.ClientHandling.NoteCommand
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
