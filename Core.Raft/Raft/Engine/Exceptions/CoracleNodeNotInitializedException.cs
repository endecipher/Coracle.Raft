using System;

namespace Coracle.Raft.Engine.Exceptions
{
    public class CoracleNodeNotInitializedException : Exception
    {
        public string NodeId { get; }
        public CoracleNodeNotInitializedException(string nodeId) : base($"Coracle: Node '{nodeId}' cannot be started as initialization is pending")
        {
            NodeId = nodeId;
        }

        public static CoracleNodeNotInitializedException New(string nodeId)
        {
            return new CoracleNodeNotInitializedException(nodeId);
        }
    }
}
