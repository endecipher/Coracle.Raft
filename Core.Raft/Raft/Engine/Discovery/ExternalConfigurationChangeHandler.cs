using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Actions.Contexts;
using TaskGuidance.BackgroundProcessing.Core;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;

namespace Coracle.Raft.Engine.Discovery
{
    internal sealed class ExternalConfigurationChangeHandler : IExternalConfigurationChangeHandler
    {
        public ExternalConfigurationChangeHandler(
            IActivityLogger activityLogger,
            IEngineConfiguration engineConfiguration,
            IClusterConfiguration clusterConfiguration,
            ICurrentStateAccessor currentStateAccessor,
            IResponsibilities responsibilities,
            IClusterConfigurationChanger clusterConfigurationChanger,
            ILeaderNodePronouncer leaderNodePronouncer,
            IGlobalAwaiter globalAwaiter,
            IPersistentProperties persistentState)
        {
            ActivityLogger = activityLogger;
            EngineConfiguration = engineConfiguration;
            ClusterConfiguration = clusterConfiguration;
            CurrentStateAccessor = currentStateAccessor;
            Responsibilities = responsibilities;
            ClusterConfigurationChanger = clusterConfigurationChanger;
            LeaderNodePronouncer = leaderNodePronouncer;
            GlobalAwaiter = globalAwaiter;
            PersistentState = persistentState;
        }

        IActivityLogger ActivityLogger { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        public IResponsibilities Responsibilities { get; }
        public IClusterConfigurationChanger ClusterConfigurationChanger { get; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; }
        public IGlobalAwaiter GlobalAwaiter { get; }
        public IPersistentProperties PersistentState { get; }

        public Task<ConfigurationChangeResult> IssueConfigurationChange(ConfigurationChangeRPC configurationChangeRPC, CancellationToken cancellationToken)
        {
            ConfigurationChangeResult result;

            var action = new OnConfigurationChangeRequestReceive(configurationChangeRPC, CurrentStateAccessor.Get(), new OnConfigurationChangeReceiveContextDependencies
            {
                EngineConfiguration = EngineConfiguration,
                ClusterConfiguration = ClusterConfiguration,
                ClusterConfigurationChanger = ClusterConfigurationChanger,
                GlobalAwaiter = GlobalAwaiter,
                LeaderNodePronouncer = LeaderNodePronouncer,
                PersistentState = PersistentState
            }, ActivityLogger);

            action.SupportCancellation();

            action.CancellationManager.Bind(cancellationToken);

            result = Responsibilities.QueueBlockingAction<ConfigurationChangeResult>(action: action, executeSeparately: false);

            return Task.FromResult(result);
        }
    }
}
