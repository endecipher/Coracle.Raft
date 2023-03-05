using System;

namespace Coracle.Raft.Engine.Exceptions
{
    public abstract class AbstractDeniedException : Exception
    {
        public AbstractDeniedException(string message) : base($"Node is currently a Candidate/Follower. Forward request to the Leader of the cluster. {message}")
        {

        }
    }
}
