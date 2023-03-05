using Coracle.Raft.Engine.Discovery;
using System;

namespace Coracle.Raft.Engine.Exceptions
{
    public class InvalidDiscoveryException : Exception
    {
        public string NodeId { get; }

        public InvalidDiscoveryException(string nodeId) : base($"Coracle: Node '{nodeId}' cannot be initialized as it is not part of the configuration returned by {nameof(IDiscoveryHandler)}")
        {
            NodeId = nodeId;
        }

        public static InvalidDiscoveryException New(string nodeId)
        {
            return new InvalidDiscoveryException(nodeId);
        }
    }
}
