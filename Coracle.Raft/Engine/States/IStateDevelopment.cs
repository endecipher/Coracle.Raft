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

using System;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States
{
    internal interface IStateDevelopment : ISystemState, IDisposable
    {
        bool IsDisposed { get; }

        StateValues StateValue { get; }

        IVolatileProperties VolatileState { get; }

        IStateChanger StateChanger { get; set; }

        /// <summary>
        /// This method is called from the <see cref="StateChanger"/>. 
        /// It is invoked at the start on the state which is scheduled to be changed.
        /// Any cleanup operations can be invoked here.
        /// 
        /// Cleanup should be fast, since this Method is invoked after the new state is initialized and ready to operate, 
        /// hence Configuring New Responsibilities should be faster than timeout inititated on New State Initialization.
        /// </summary>
        /// <returns>And awaitable Task</returns>
        Task OnStateChangeBeginDisposal();

        /// <summary>
        /// This method is called from the <see cref="StateChanger"/>. 
        /// It is invoked on the new state just after dependencies have been filled.
        /// Any lightweight fast startup operations can be invoked here.
        /// 
        /// <see cref="IVolatileProperties"/> are passed on from Old State 
        /// </summary>
        /// <returns>And awaitable Task</returns>
        Task InitializeOnStateChange(IVolatileProperties volatileProperties);

        /// <summary>
        /// This method is called from the <see cref="StateChanger"/>. 
        /// It is invoked once the new state has replaced the old state in <see cref="ICurrentStateAccessor"/>
        /// </summary>
        /// <returns>And awaitable Task</returns>
        Task OnStateEstablishment();
    }
}
