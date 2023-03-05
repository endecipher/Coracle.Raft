#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Core;
using System.Collections.Generic;
using System.Linq;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Actions;
using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    internal sealed class MembershipChanger : IMembershipChanger
    {
        #region Constants

        public const string Entity = nameof(ClusterConfiguration);
        public const string JointConsensusTriggered = nameof(JointConsensusTriggered);
        public const string ApplyingConfiguration = nameof(ApplyingConfiguration);
        public const string EventKey = nameof(EventKey);

        #endregion

        public MembershipChanger(
            IClusterConfiguration clusterConfiguration,
            ILeaderNodePronouncer leaderNodePronouncer,
            ICurrentStateAccessor currentStateAccessor,
            IGlobalAwaiter globalAwaiter,
            IResponsibilities responsibilities,
            IActivityLogger activityLogger,
            IEngineConfiguration engineConfiguration)
        {
            ClusterConfiguration = clusterConfiguration;
            LeaderNodePronouncer = leaderNodePronouncer;
            CurrentStateAccessor = currentStateAccessor;
            GlobalAwaiter = globalAwaiter;
            Responsibilities = responsibilities;
            ActivityLogger = activityLogger;
            EngineConfiguration = engineConfiguration;
        }

        IClusterConfiguration ClusterConfiguration { get; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        IResponsibilities Responsibilities { get; }
        IActivityLogger ActivityLogger { get; }
        IEngineConfiguration EngineConfiguration { get; }

        public bool IsThisNodePartOfCluster => ClusterConfiguration.IsThisNodePartOfCluster;

        /// <summary>
        /// Whenever a node receives a log entry (which is a LogEntry of type Configuration), they would apply it to their node immediately.
        /// This implementation is how the Node's cluster configuration changes.
        /// </summary>
        public void ChangeMembership(MembershipUpdateEvent membershipChange, bool tryForReplication = false, bool isInstallingSnapshot = false)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = ApplyingConfiguration,
                Level = ActivityLogLevel.Debug
            }
            .WithCallerInfo());

            string thisNodeId = EngineConfiguration.NodeId;

            bool isThisNodeLeader = LeaderNodePronouncer.IsLeaderRecognized && LeaderNodePronouncer.RecognizedLeaderConfiguration.UniqueNodeId.Equals(thisNodeId);
            bool isThisNodeInNewCluster = membershipChange.Configuration.Any(x => x.UniqueNodeId.Equals(thisNodeId));

            /// Once the entire snapshot is received, if this node is not present in the snapshot's cluster configuration, 
            /// then it is safe to assume, that the snapshot represents old configuration.
            /// 
            /// Once the entire snapshot is received, then appendEntries will continue normally, and then we would receive the last configuration entry.
            /// The last/most recent configuration from the leader can then be parsed and then applied to the node within that AppendEntries call
            if (isInstallingSnapshot && !isThisNodeInNewCluster)
            {
                return;
            }

            if (!isThisNodeInNewCluster)
            {
                if (!isThisNodeLeader)
                {
                    /// Update configuration manually
                    /// For any node who is not a part of the configuration anymore, they can drop off

                    ClusterConfiguration.UpdateConfiguration(thisNodeId, membershipChange.Configuration);
                    (CurrentStateAccessor.Get() as IMembershipUpdate)?.UpdateMembership(ClusterConfiguration.Peers);

                    /// Decommission this Node to stop all processing if not a part of the configuration and not a leader
                    CurrentStateAccessor.Get()?.Decommission();
                }
                else
                {
                    /// During C-old,new : we can be sure that the Leader node will be a part of the Configuration, since C-old,new is a joint consensus,
                    /// and is a union of all servers.
                    /// However, during the C-new phase, and once after all the new servers are up-to-date, 
                    ///     if this node is the leader of the cluster, then we can't decommission it unless we commit C-new first.

                    var action = new OnAwaitDecommission(membershipChange.ConfigurationLogEntryIndex, membershipChange.Configuration, new OnAwaitDecommissionContextDependencies
                    {
                        ClusterConfiguration = ClusterConfiguration,
                        CurrentStateAccessor = CurrentStateAccessor,
                        GlobalAwaiter = GlobalAwaiter,
                        EngineConfiguration = EngineConfiguration
                    }, ActivityLogger);

                    action.SupportCancellation();

                    Responsibilities.QueueAction(action, executeSeparately: false);
                }
            }
            else
            {

                if (isThisNodeLeader && tryForReplication)
                {
                    /// Wait for replication/heartbeat. 
                    /// This will try to attempt to ensure that the nodes scheduled for replication (if available), also get the conf C-new entry replicated. 
                    /// The abandoning nodes who would still be up, might have the C-new entry replicated which would help them decommission properly. 
                    /// This wouldn't take too long, as it should take as much time as a heartbeat
                    GlobalAwaiter.AwaitNoDeposition(System.Threading.CancellationToken.None);
                }

                /// Update configuration manually
                /// For any node who is not a part of the configuration anymore, they can drop off

                ClusterConfiguration.UpdateConfiguration(thisNodeId, membershipChange.Configuration);
                (CurrentStateAccessor.Get() as IMembershipUpdate)?.UpdateMembership(ClusterConfiguration.Peers);
            }
        }

        /// <summary>
        /// Joint Consensus should return C-old,new.
        /// Thus, it should return the union of old and new configurations.
        /// </summary>
        public IEnumerable<NodeChangeConfiguration> CalculateJointConsensusConfigurationWith(IEnumerable<NodeConfiguration> newConfigurationNodes)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = JointConsensusTriggered,
                Level = ActivityLogLevel.Debug
            }
            .WithCallerInfo());

            var jointConsensus = new Dictionary<string, NodeChangeConfiguration>();
            var currentNodes = ClusterConfiguration.CurrentConfiguration;

            void TryAdd(string key, NodeChangeConfiguration nodeConfigToAdd)
            {
                if (jointConsensus.TryGetValue(key, out var config))
                {
                    config.IsNew |= nodeConfigToAdd.IsNew;
                    config.IsOld |= nodeConfigToAdd.IsOld;
                }
                else
                {
                    jointConsensus.Add(key, nodeConfigToAdd);
                }
            }

            foreach (var node in currentNodes)
            {
                TryAdd(node.UniqueNodeId, new NodeChangeConfiguration
                {
                    UniqueNodeId = node.UniqueNodeId,
                    BaseUri = node.BaseUri,
                    IsOld = true,
                    IsNew = false,
                });
            }

            foreach (var node in newConfigurationNodes)
            {
                TryAdd(node.UniqueNodeId, new NodeChangeConfiguration
                {
                    UniqueNodeId = node.UniqueNodeId,
                    BaseUri = node.BaseUri,
                    IsOld = false,
                    IsNew = true,
                });
            }

            return jointConsensus.Values;
        }

        public bool HasNodeBeenRemoved(string externalNodeId)
        {
            return ClusterConfiguration.GetPeerNodeConfiguration(externalNodeId) == null;
        }
    }
}
