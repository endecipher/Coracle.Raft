using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Configuration
{
    /// <summary>
    /// This interface is implemented by any class which needs to handle a configuration change
    /// </summary>
    internal interface IHandleConfigurationChange
    {
        void HandleConfigurationChange(IEnumerable<INodeConfiguration> newPeerNodeConfigurations);
    }
}
