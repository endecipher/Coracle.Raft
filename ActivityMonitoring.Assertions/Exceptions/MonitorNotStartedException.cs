using System.Runtime.Serialization;

namespace ActivityMonitoring.Assertions.Core
{
    [Serializable]
    internal class MonitorNotStartedException : Exception
    {
        public MonitorNotStartedException()
        {
        }

        public MonitorNotStartedException(string message) : base(message)
        {
        }

        public MonitorNotStartedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MonitorNotStartedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}