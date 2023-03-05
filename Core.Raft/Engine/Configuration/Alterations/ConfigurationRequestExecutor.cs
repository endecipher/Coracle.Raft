using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Actions.Contexts;
using TaskGuidance.BackgroundProcessing.Core;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Actions;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    internal sealed class ConfigurationRequestExecutor : IConfigurationRequestExecutor
    {
        public ConfigurationRequestExecutor(
            IActivityLogger activityLogger,
            IEngineConfiguration engineConfiguration,
            IClusterConfiguration clusterConfiguration,
            ICurrentStateAccessor currentStateAccessor,
            IResponsibilities responsibilities,
            IMembershipChanger clusterConfigurationChanger,
            ILeaderNodePronouncer leaderNodePronouncer,
            IGlobalAwaiter globalAwaiter,
            IPersistentStateHandler persistentState)
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
        public IMembershipChanger ClusterConfigurationChanger { get; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; }
        public IGlobalAwaiter GlobalAwaiter { get; }
        public IPersistentStateHandler PersistentState { get; }

        public Task<ConfigurationChangeResult> IssueChange(ConfigurationChangeRequest configurationChangeRequest, CancellationToken cancellationToken)
        {
            ConfigurationChangeResult result;

            var action = new OnConfigurationChangeRequestReceive(configurationChangeRequest, CurrentStateAccessor.Get(), new OnConfigurationChangeReceiveContextDependencies
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
