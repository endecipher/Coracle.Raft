using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Responsibilities;
using System.Collections.Generic;
using System.Linq;

namespace Core.Raft.Canoe.Engine.Configuration
{
    internal sealed class ClusterConfigurationChanger : IClusterConfigurationChanger
    {
        #region Constants

        public const string Entity = nameof(ClusterConfiguration);
        public const string JointConsensusTriggered = nameof(JointConsensusTriggered);
        public const string ApplyingConfiguration = nameof(ApplyingConfiguration);
        public const string EventKey = nameof(EventKey);

        #endregion

        public ClusterConfigurationChanger(
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
        public IEngineConfiguration EngineConfiguration { get; }

        /// <summary>
        /// Whenever a node receives a log entry (which is a <see cref="ConfigurationLogEntry"/>), they would apply it to their node immediately.
        /// This implementation is how the Node's cluster configuration changes.
        /// </summary>
        /// <param name="membershipChange"></param>
        /// <returns></returns>
        public void ApplyConfiguration(ClusterMembershipChange membershipChange)
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
            bool isThisNodePartOfTheNewConfiguration = membershipChange.Configuration.Any(x => x.UniqueNodeId.Equals(thisNodeId));

            /// 
            /// Update configuration manually
            /// For any node who is not a part of the configuration anymore, they can drop off

            ClusterConfiguration.UpdateConfiguration(thisNodeId, membershipChange.Configuration);
            (CurrentStateAccessor.Get() as IHandleConfigurationChange)?.HandleConfigurationChange(ClusterConfiguration.Peers);

            if (!isThisNodePartOfTheNewConfiguration)
            {
                if (!isThisNodeLeader)
                {
                    /// Decomission this Node to stop all processing if not a part of the configuration and not a leader
                    CurrentStateAccessor.Get()?.Decomission();
                }
                else
                {
                    /// During C-old,new : we can be sure that the Leader node will be a part of the Configuration, since C-old,new is a joint consensus,
                    /// and is a union of all servers.
                    /// However, during the C-new phase, and once after all the new servers are up-to-date, 
                    ///     if this node is the leader of the cluster, then we can't decomission it unless we commit C-new first.
                    ///     

                    var action = new OnAwaitDecomission(membershipChange.ConfigurationLogEntryIndex, new OnAwaitDecomissionContextDependencies
                    {
                        ClusterConfiguration = ClusterConfiguration,
                        CurrentStateAccessor = CurrentStateAccessor,
                        GlobalAwaiter = GlobalAwaiter
                    }, ActivityLogger);

                    action.SupportCancellation();

                    Responsibilities.QueueEventAction(action, executeSeparately: false);
                }
            }
        }

        /// <summary>
        /// Joint Consensus should return C-old,new.
        /// Thus, it should return the union of old and new configurations.
        /// </summary>
        /// <param name="newConfiguration"></param>
        /// <returns></returns>
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

    }
}
