using ActivityLogger.Logging;
using Coracle.Raft.Engine.ActivityLogger;
using Coracle.Raft.Engine.Discovery;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    internal sealed class LeaderNodePronouncer : ILeaderNodePronouncer
    {
        #region Constants

        public const string CannotRecognizeAsNotInCluster = nameof(CannotRecognizeAsNotInCluster);
        public const string CannotRecognizeItselfAsNotInCluster = nameof(CannotRecognizeItselfAsNotInCluster);
        public const string LeaderRecognizedAsItself = nameof(LeaderRecognizedAsItself);
        public const string LeaderRecognized = nameof(LeaderRecognized);
        public const string Entity = nameof(LeaderNodePronouncer);
        public const string nodeIdToUpdate = nameof(nodeIdToUpdate);

        #endregion

        public LeaderNodePronouncer(IClusterConfiguration clusterConfiguration, IDiscoveryHandler discoverer, IActivityLogger activityLogger)
        {
            ClusterConfiguration = clusterConfiguration;
            Discoverer = discoverer;
            ActivityLogger = activityLogger;
        }

        public INodeConfiguration RecognizedLeaderConfiguration { get; private set; } = null;
        public IClusterConfiguration ClusterConfiguration { get; }
        public IDiscoveryHandler Discoverer { get; }
        public IActivityLogger ActivityLogger { get; }

        public void SetNewLeader(string leaderServerId)
        {
            var configuration = ClusterConfiguration.GetPeerNodeConfiguration(leaderServerId);

            if (configuration != null)
            {
                RecognizedLeaderConfiguration = configuration;

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = LeaderRecognized,
                    Level = ActivityLogLevel.Information,
                }
                .With(ActivityParam.New(nodeIdToUpdate, leaderServerId))
                .WithCallerInfo());

                return;
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = CannotRecognizeAsNotInCluster,
                    Level = ActivityLogLevel.Error,
                }
                .With(ActivityParam.New(nodeIdToUpdate, leaderServerId))
                .WithCallerInfo());
            }
        }

        public void SetRunningNodeAsLeader()
        {
            if (!ClusterConfiguration.IsThisNodePartOfCluster)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = CannotRecognizeItselfAsNotInCluster,
                    Level = ActivityLogLevel.Error,
                }
                .WithCallerInfo());
            }

            RecognizedLeaderConfiguration = ClusterConfiguration.ThisNode;

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = LeaderRecognizedAsItself,
                Level = ActivityLogLevel.Information,
            }
            .WithCallerInfo());
        }
    }
}
