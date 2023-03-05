using Coracle.Raft.Engine.Node;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Coracle.Raft.Engine.Exceptions
{
    public class EngineConfigurationInvalidException : Exception
    {
        public IEnumerable<string> ErrorsEncountered { get; }
        public string NodeId { get; }

        public EngineConfigurationInvalidException(string nodeId, IEnumerable<string> errorsEncountered) : base($"Coracle: Node '{nodeId}' cannot be initialized as {nameof(IEngineConfiguration)} has the following errors: {errorsEncountered.Aggregate(string.Concat)}")
        {
            NodeId = nodeId;
            ErrorsEncountered = errorsEncountered;
        }

        public static EngineConfigurationInvalidException New(string nodeId, IEnumerable<string> errors)
        {
            return new EngineConfigurationInvalidException(nodeId, errors);
        }
    }
}
