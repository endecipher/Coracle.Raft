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
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnAwaitDecommissionContextDependencies
    {
        internal IClusterConfiguration ClusterConfiguration { get; set; }
        internal ICurrentStateAccessor CurrentStateAccessor { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
    }

    internal sealed class OnAwaitDecommissionContext : IActionContext
    {
        public OnAwaitDecommissionContext(OnAwaitDecommissionContextDependencies dependencies) : base()
        {
            Dependencies = dependencies;
        }

        internal long ConfigurationLogEntryIndex { get; set; }
        internal IEnumerable<NodeConfiguration> NewConfiguration { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State => CurrentStateAccessor.Get();
        OnAwaitDecommissionContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        public IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        public IClusterConfiguration ClusterConfiguration => Dependencies.ClusterConfiguration;
        public ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;
        public IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;

        #endregion
    }
}
