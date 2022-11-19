using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Discovery
{
    public interface IDiscoveryOperation
    {
        IEnumerable<NodeConfiguration> AllNodes { get; }

        bool IsOperationSuccessful { get; }

        Exception Exception { get; }
    }
}
