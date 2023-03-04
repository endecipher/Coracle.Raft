namespace Coracle.Raft.Engine.Exceptions
{
    public class ConfigurationChangeDeniedException : AbstractDeniedException
    {
        public ConfigurationChangeDeniedException(string message) : base(message)
        {

        }

        public static ConfigurationChangeDeniedException New()
        {
            return new ConfigurationChangeDeniedException(string.Empty);
        }
    }
}
