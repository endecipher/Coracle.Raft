using System;

namespace Coracle.Raft.Engine.Exceptions
{
    public class ClientCommandDeniedException : Exception
    {
        public ClientCommandDeniedException(string message) : base(message)
        {

        }

        public static ClientCommandDeniedException New()
        {
            return new ClientCommandDeniedException("This Node is currently in Candidate/Follower state. Please forwarding to leader. ");
        }
    }
}
