using System.Runtime.Serialization;

namespace ActivityMonitoring.Assertions.Core
{
    [Serializable]
    internal class QueueIdNotFoundException : Exception
    {
        private Guid contextQueueId;

        public QueueIdNotFoundException()
        {
        }

        public QueueIdNotFoundException(Guid contextQueueId)
        {
            this.contextQueueId = contextQueueId;
        }

        public QueueIdNotFoundException(string message) : base(message)
        {
        }

        public QueueIdNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected QueueIdNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}