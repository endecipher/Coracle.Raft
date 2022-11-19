using System.Runtime.Serialization;

namespace ActivityMonitoring.Assertions.Core
{
    [Serializable]
    internal class ReachedMaximumContextQueueInstancesException : Exception
    {
        public ReachedMaximumContextQueueInstancesException()
        {
        }

        public ReachedMaximumContextQueueInstancesException(string message) : base(message)
        {
        }

        public ReachedMaximumContextQueueInstancesException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ReachedMaximumContextQueueInstancesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}