using Coracle.Web.ClientHandling.Notes;

namespace Coracle.Web.ClientHandling.NoteCommand
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
