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
using Coracle.Raft.Engine.ActivityLogger;
using System.Threading;
using Coracle.Raft.Engine.Node;

namespace Coracle.Raft.Engine.Helper
{
    internal sealed class HeartbeatTimer : IHeartbeatTimer
    {
        #region Constants

        public const string InitiatingCallback = nameof(InitiatingCallback);
        public const string HeartbeatTimerEntity = nameof(HeartbeatTimer);
        public const string TimeOut = nameof(TimeOut);

        #endregion

        public HeartbeatTimer(IEngineConfiguration engineConfiguration, IActivityLogger activityLogger)
        {
            EngineConfiguration = engineConfiguration;
            ActivityLogger = activityLogger;
        }

        IEngineConfiguration EngineConfiguration { get; }
        IActivityLogger ActivityLogger { get; }
        public int TimeOutInMilliseconds => EngineConfiguration.HeartbeatInterval_InMilliseconds;

        public Timer Timer = null;

        public void RegisterNew(TimerCallback timerCallback)
        {
            Timer = new Timer((state) =>
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = HeartbeatTimerEntity,
                    Event = InitiatingCallback,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(TimeOut, TimeOutInMilliseconds))
                .WithCallerInfo());

                timerCallback.Invoke(state);

            }, null, TimeOutInMilliseconds, TimeOutInMilliseconds);
        }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
