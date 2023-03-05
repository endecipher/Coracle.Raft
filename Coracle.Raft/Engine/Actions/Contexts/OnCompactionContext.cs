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

using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnCompactionContextDependencies
    {
        public IEngineConfiguration EngineConfiguration { get; set; }
        public IPersistentStateHandler PersistentState { get; set; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
        public IResponsibilities Responsibilities { get; set; }
        public IClusterConfiguration ClusterConfiguration { get; set; }
    }

    internal sealed class OnCompactionContext : IActionContext
    {
        public OnCompactionContext(OnCompactionContextDependencies dependencies, IStateDevelopment state)
        {
            Dependencies = dependencies;
            State = state;
        }

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State { get; }
        OnCompactionContextDependencies Dependencies { get; set; }

        #region Action Dependencies

        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IResponsibilities Responsibilities => Dependencies.Responsibilities;
        internal IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
