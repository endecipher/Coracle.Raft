using Core.Raft.Canoe.Engine.Command;

namespace Coracle.Web.ClientHandling.NoteCommand
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
