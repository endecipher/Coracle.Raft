using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    /// <summary>
    /// This interface is implemented by any class which needs to handle a configuration change/update once <see cref="IClusterConfiguration"/> updates
    /// </summary>
    internal interface IMembershipUpdate
    {
        void UpdateMembership(IEnumerable<INodeConfiguration> newPeerNodeConfigurations);
    }
}
