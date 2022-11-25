using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Logs;
using System.Collections.Generic;

namespace Core.Raft.Canoe.Engine.Configuration
{
    public interface IClusterConfigurationChanger
    {

        #region Configuration Change

        IEnumerable<NodeChangeConfiguration> CalculateJointConsensusConfigurationWith(IEnumerable<NodeConfiguration> newConfiguration);

        void ApplyConfiguration(ClusterMembershipChange membershipChange);

        #endregion 

    }
}
