using System.Runtime.Serialization;

namespace ActivityMonitoring.Assertions.Core
{
    [Serializable]
    internal class AlreadyStoppedException : Exception
    {
        public AlreadyStoppedException()
        {
        }

        public AlreadyStoppedException(string message) : base(message)
        {
        }

        public AlreadyStoppedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AlreadyStoppedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}