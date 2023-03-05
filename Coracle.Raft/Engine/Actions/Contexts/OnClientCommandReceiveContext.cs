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

using Coracle.Raft.Engine.Command;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal sealed class OnClientCommandReceiveContextDependencies
    {
        internal IAppendEntriesManager AppendEntriesManager { get; set; }
        internal IEngineConfiguration EngineConfiguration { get; set; }
        internal IPersistentStateHandler PersistentState { get; set; }
        internal IStateMachineHandler ClientRequestHandler { get; set; }
        internal IGlobalAwaiter GlobalAwaiter { get; set; }
        internal ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
    }

    internal sealed class OnClientCommandReceiveContext<TCommand> : IActionContext where TCommand : class, ICommand
    {
        public OnClientCommandReceiveContext(IStateDevelopment state, OnClientCommandReceiveContextDependencies dependencies) : base()
        {
            State = state;
            Dependencies = dependencies;
        }

        internal TCommand Command { get; set; }
        internal string UniqueCommandId { get; set; }
        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal IStateDevelopment State { get; set; }
        OnClientCommandReceiveContextDependencies Dependencies { get; set; }

        #region Action Dependencies
        public IStateMachineHandler ClientRequestHandler => Dependencies.ClientRequestHandler;
        public IGlobalAwaiter GlobalAwaiter => Dependencies.GlobalAwaiter;
        public IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        public ILeaderNodePronouncer LeaderNodePronouncer => Dependencies.LeaderNodePronouncer;
        public IPersistentStateHandler PersistentState => Dependencies.PersistentState;
        public IAppendEntriesManager AppendEntriesManager => Dependencies.AppendEntriesManager;

        #endregion

        public void Dispose()
        {
            Command = null;
            State = null;
            Dependencies = null;
        }
    }
}
