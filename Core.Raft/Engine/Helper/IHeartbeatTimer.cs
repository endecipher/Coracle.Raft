using System;
using System.Threading;

namespace Coracle.Raft.Engine.Helper
{
    internal interface IHeartbeatTimer : IDisposable
    {
        void RegisterNew(TimerCallback timerCallback);
    }
}
