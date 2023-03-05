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
