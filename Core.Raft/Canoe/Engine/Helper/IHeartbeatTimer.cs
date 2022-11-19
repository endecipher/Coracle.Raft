using System;
using System.Threading;

namespace Core.Raft.Canoe.Engine.Helper
{
    internal interface IHeartbeatTimer : IDisposable
    {
        void RegisterNew(TimerCallback timerCallback);
    }
}
