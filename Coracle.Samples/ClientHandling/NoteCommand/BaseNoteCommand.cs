using Coracle.Raft.Engine.ClientHandling.Command;

namespace Coracle.Samples.ClientHandling.NoteCommand
{
    public abstract class BaseNoteCommand : ICommand
    {
        public BaseNoteCommand()
        {
            UniqueId = Guid.NewGuid().ToString();
        }

        public string UniqueId { get; private set; }

        public abstract bool IsReadOnly { get; }

        public abstract string Type { get; }

        public object Data { get; protected set; }

        public void Dispose()
        {

        }
    }
}
