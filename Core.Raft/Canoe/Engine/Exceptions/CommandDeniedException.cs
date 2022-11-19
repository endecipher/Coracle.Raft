using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Exceptions
{
    public class ClientCommandDeniedException : Exception
    {
        public ClientCommandDeniedException(string message) : base(message)
        {

        }

        public static ClientCommandDeniedException New()
        {
            return new ClientCommandDeniedException("This Node is currently in Candidate/Follower state. Forwarding to Leader. ");
        }
    }
}
