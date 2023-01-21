using Coracle.Raft.Engine.Configuration.Cluster;
using System;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Discovery
{
    public interface IDiscoveryOperation
    {
        IEnumerable<NodeConfiguration> AllNodes { get; }

        bool IsOperationSuccessful { get; }

        Exception Exception { get; }
    }
}
