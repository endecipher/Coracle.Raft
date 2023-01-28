using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Generic;
using Coracle.Raft.Engine.Configuration;

namespace Coracle.Raft.Engine.Actions.Awaiters
{
    internal class OnAwaitDecomission : BaseAction<OnAwaitDecomissionContext, object>
    {
        public OnAwaitDecomission(long configurationLogEntryIndex, IEnumerable<NodeConfiguration> newClusterConfig, OnAwaitDecomissionContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnAwaitDecomissionContext(actionDependencies)
        {
            ConfigurationLogEntryIndex = configurationLogEntryIndex,
            NewConfiguration = newClusterConfig,
            InvocationTime = DateTimeOffset.UtcNow,
        }, activityLogger)
        {
        }

        public override TimeSpan TimeOut => TimeSpan.MaxValue;

        public override string UniqueName => nameof(OnAwaitDecomission);

        protected override Task<object> Action(CancellationToken cancellationToken)
        {
            Input.GlobalAwaiter.AwaitEntryCommit(Input.ConfigurationLogEntryIndex, cancellationToken);

            /// Update leader configuration to exclude itself, only after C-new entry is committed and replicated across majority. 
            Input.ClusterConfiguration.UpdateConfiguration(Input.EngineConfiguration.NodeId, Input.NewConfiguration);
            (Input.CurrentStateAccessor.Get() as IHandleConfigurationChange)?.HandleConfigurationChange(Input.ClusterConfiguration.Peers);

            /// Separate conditional, since we want to verify post await, the same before we decomission
            Input.CurrentStateAccessor.Get()?.Decommission();

            return null;
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }
    }
}
