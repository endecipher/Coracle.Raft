namespace Coracle.Raft.Engine.Exceptions
{
    public class ClientCommandDeniedException : AbstractDeniedException
    {
        public ClientCommandDeniedException(string message) : base(message)
        {

        }

        public static ClientCommandDeniedException New()
        {
            return new ClientCommandDeniedException(string.Empty);
        }
    }
}
