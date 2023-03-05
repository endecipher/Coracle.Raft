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
