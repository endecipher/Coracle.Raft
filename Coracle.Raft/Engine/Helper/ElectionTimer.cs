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
using System;
using System.Threading;

namespace Coracle.Raft.Engine.Helper
{
    internal sealed class ElectionTimer : IElectionTimer
    {
        IEngineConfiguration Config { get; }

        public Timer Timer = null;

        public ElectionTimer(IEngineConfiguration engineConfiguration)
        {
            Config = engineConfiguration;
        }

        public void RegisterNew(TimerCallback timerCallback)
        {
            var timeOutInMilliseconds = Convert.ToInt32(RandomTimeout.TotalMilliseconds);

            Timer = new Timer(timerCallback, null, timeOutInMilliseconds, timeOutInMilliseconds);
        }

        /// <summary>
        /// Raft uses randomized election timeouts to ensure that
        /// split votes are rare and that they are resolved quickly.To
        /// prevent split votes in the first place, election timeouts are
        /// chosen randomly from a fixed interval (e.g., 150–300ms).
        /// 
        /// <see cref="Section 5.2 Leader Election"/>
        /// </summary>
        public void ResetWithDifferentTimeout()
        {
            var timeOutInMilliseconds = Convert.ToInt32(RandomTimeout.TotalMilliseconds);

            Timer.Change(timeOutInMilliseconds, timeOutInMilliseconds);
        }

        public TimeSpan RandomTimeout => TimeSpan.FromMilliseconds(new Random()
            .Next(Config.MinElectionTimeout_InMilliseconds, Config.MaxElectionTimeout_InMilliseconds));

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
