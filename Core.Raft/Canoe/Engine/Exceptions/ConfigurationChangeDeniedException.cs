using System;

namespace Core.Raft.Canoe.Engine.Exceptions
{
    public class ConfigurationChangeDeniedException : Exception
    {
        public ConfigurationChangeDeniedException(string message) : base(message)
        {

        }

        public static ConfigurationChangeDeniedException New()
        {
            return new ConfigurationChangeDeniedException("This Node is currently in Candidate/Follower state. Forwarding to Leader. ");
        }
    }

}
