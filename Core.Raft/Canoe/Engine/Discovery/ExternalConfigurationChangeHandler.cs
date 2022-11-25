using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Responsibilities;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Discovery
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

            result = Responsibilities.QueueBlockingEventAction<ConfigurationChangeResult>(action: action, executeSeparately: false);

            return Task.FromResult(result);
        }
    }
}
