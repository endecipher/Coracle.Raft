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

using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.States;

namespace Coracle.Raft.Engine.Actions.Contexts
{

    internal sealed class OnAwaitEntryCommitContextDependencies
    {
        public ICurrentStateAccessor CurrentStateAccessor { get; set; }
        public IPersistentStateHandler PersistentState { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }
    }


    internal sealed class OnAwaitEntryCommitContext : IActionContext
    {
        public OnAwaitEntryCommitContext(long logEntryIndex, OnAwaitEntryCommitContextDependencies dependencies)
        {
            LogEntryIndex = logEntryIndex;
            Dependencies = dependencies;
        }

        public bool IsContextValid => !State.IsDisposed && !State.StateValue.IsAbandoned() && !State.StateValue.IsStopped();
        internal long LogEntryIndex { get; }
        internal IStateDevelopment State => CurrentStateAccessor.Get();
        OnAwaitEntryCommitContextDependencies Dependencies { get; set; }

        #region Action Dependencies

        internal ICurrentStateAccessor CurrentStateAccessor => Dependencies.CurrentStateAccessor;
        internal IEngineConfiguration EngineConfiguration => Dependencies.EngineConfiguration;
        internal IPersistentStateHandler PersistentState => Dependencies.PersistentState;

        #endregion

        public void Dispose()
        {
            Dependencies = null;
        }
    }
}
