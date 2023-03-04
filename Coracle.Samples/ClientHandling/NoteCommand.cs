using Coracle.Raft.Engine.Command;

namespace Coracle.Samples.ClientHandling
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
            command.Type = NoteStorage.AddNote;
            command.Data = data;

            return command;
        }

        public static NoteCommand CreateGet(Note data)
        {
            var command = new NoteCommand();

            command.IsReadOnly = true;
            command.Type = NoteStorage.GetNote;
            command.Data = data;

            return command;
        }
    }
}
