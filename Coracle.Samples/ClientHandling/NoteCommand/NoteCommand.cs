using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Samples.ClientHandling.Notes;

namespace Coracle.Samples.ClientHandling.NoteCommand
{
    public class NoteCommand : ICommand
    {
        public NoteCommand()
        {

        }

        public string UniqueId { get; set; } = Guid.NewGuid().ToString();

        public bool IsReadOnly { get; set; }

        public string Type { get; set; }

        public Note Data { get; set; }

        public static NoteCommand CreateAdd(Note data)
        {
            var command = new NoteCommand();

            command.IsReadOnly = false;
            command.Type = Notes.Notes.AddNote;
            command.Data = data;
            
            return command;
        }

        public static NoteCommand CreateGet(Note data)
        {
            var command = new NoteCommand();

            command.IsReadOnly = true;
            command.Type = Notes.Notes.GetNote;
            command.Data = data;

            return command;
        }
    }
}
