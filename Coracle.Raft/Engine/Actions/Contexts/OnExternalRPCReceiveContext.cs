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
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnExternalRPCReceiveContextDependencies
    {
        internal IMembershipChanger ClusterConfigurationChanger { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnExternalRPCReceiveContext<T> : IActionContext where T : IRemoteCall
    {
        public OnExternalRPCReceiveContext(IStateDevelopment state, OnExternalRPCReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal T Request { get; set; }
        internal IStateDevelopment State { get; set; }
        OnExternalRPCReceiveContextDependencies Dependencies { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal bool TurnToFollower { get; set; } = false;

        #region Action Dependencies
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        internal ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        internal IMembershipChanger ClusterConfigurationChanger => Dependencies.ClusterConfigurationChanger;

        #endregion

        public void Dispose()
        {
            Request = default;
            State = null;
            Dependencies = null;
        }
    }
}
