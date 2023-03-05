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

using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnConfigurationChangeReceiveContextDependencies
    {
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal IMembershipChanger ClusterConfigurationChanger { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnConfigurationChangeReceiveContext : IActionContext
    {
        public OnConfigurationChangeReceiveContext(IStateDevelopment state, OnConfigurationChangeReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal ConfigurationChangeRequest ConfigurationChange { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State { get; set; }
        OnConfigurationChangeReceiveContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        internal IMembershipChanger ClusterConfigurationChanger => Dependencies.ClusterConfigurationChanger;
        #endregion

        public void Dispose()
        {
            Dependencies = null;
            ConfigurationChange = null;
            State = null;
        }
    }
}
