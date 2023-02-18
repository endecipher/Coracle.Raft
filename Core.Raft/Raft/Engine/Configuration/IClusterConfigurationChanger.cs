using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Logs;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration
{
    public interface IClusterConfigurationChanger
    {

        #region Configuration Change

        IEnumerable<NodeChangeConfiguration> CalculateJointConsensusConfigurationWith(IEnumerable<NodeConfiguration> newConfiguration);

        void ApplyConfiguration(ClusterMembershipChange membershipChange, bool tryForReplication = false, bool isInstallingSnapshot = false);

        bool HasNodeBeenRemoved(string externalNodeId);

        #endregion 

    }
}
