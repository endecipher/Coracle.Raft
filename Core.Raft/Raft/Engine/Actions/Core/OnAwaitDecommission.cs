using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Configuration.Cluster;
using System.Collections.Generic;
using Coracle.Raft.Engine.Configuration.Alterations;

namespace Coracle.Raft.Engine.Actions.Core
{
    internal class OnAwaitDecommission : BaseAction<OnAwaitDecommissionContext, object>
    {
        public OnAwaitDecommission(long configurationLogEntryIndex, IEnumerable<NodeConfiguration> newClusterConfig, OnAwaitDecommissionContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnAwaitDecommissionContext(actionDependencies)
        {
            ConfigurationLogEntryIndex = configurationLogEntryIndex,
            NewConfiguration = newClusterConfig,

        }, activityLogger)
        {
        }

        public override TimeSpan TimeOut => TimeSpan.MaxValue;
        public override string UniqueName => nameof(OnAwaitDecommission);

        protected override Task<object> Action(CancellationToken cancellationToken)
        {
            Input.GlobalAwaiter.AwaitEntryCommit(Input.ConfigurationLogEntryIndex, cancellationToken);

            /// Update leader configuration to exclude itself, only after C-new entry is committed and replicated across majority 
            Input.ClusterConfiguration.UpdateConfiguration(Input.EngineConfiguration.NodeId, Input.NewConfiguration);
            (Input.CurrentStateAccessor.Get() as IMembershipUpdate)?.UpdateMembership(Input.ClusterConfiguration.Peers);

            /// Separate conditional, since we want to verify post await, the same before we decommission
            Input.CurrentStateAccessor.Get()?.Decommission();

            return null;
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }
    }
}
