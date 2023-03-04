using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    public interface IMembershipChanger
    {
        bool IsThisNodePartOfCluster { get; }

        #region Configuration Change

        IEnumerable<NodeChangeConfiguration> CalculateJointConsensusConfigurationWith(IEnumerable<NodeConfiguration> newConfiguration);

        void ChangeMembership(MembershipUpdateEvent membershipChange, bool tryForReplication = false, bool isInstallingSnapshot = false);

        bool HasNodeBeenRemoved(string externalNodeId);

        #endregion 
    }
}
