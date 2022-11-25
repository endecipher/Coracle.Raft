﻿using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using EventGuidance.Structure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions.Awaiters
{
    internal class OnAwaitDecomission : EventAction<OnAwaitDecomissionContext, object>
    {
        public OnAwaitDecomission(long configurationLogEntryIndex, OnAwaitDecomissionContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnAwaitDecomissionContext(actionDependencies)
        {
            ConfigurationLogEntryIndex = configurationLogEntryIndex,
            InvocationTime = DateTimeOffset.UtcNow,
        }, activityLogger)
        {
        }

        public override TimeSpan TimeOut => TimeSpan.MaxValue;

        public override string UniqueName => nameof(OnAwaitDecomission);

        protected override Task<object> Action(CancellationToken cancellationToken)
        {
            if (!Input.ClusterConfiguration.IsThisNodePartOfCluster)
            {
                Input.GlobalAwaiter.AwaitEntryCommit(Input.ConfigurationLogEntryIndex, cancellationToken);
            }

            // Separate conditional, since we want to verify post await, the same before we decomission
            if (!Input.ClusterConfiguration.IsThisNodePartOfCluster) 
            {
                Input.CurrentStateAccessor.Get()?.Decomission();
            }
            return null;
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }
    }
}
