using System;
using System.Runtime.Serialization;

namespace Coracle.Raft.Engine.Exceptions
{
    [Serializable]
    internal class CommandAlreadyInQueueException : Exception
    {
        public CommandAlreadyInQueueException()
        {
        }

        public CommandAlreadyInQueueException(string message) : base(message)
        {
        }

        public CommandAlreadyInQueueException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CommandAlreadyInQueueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}