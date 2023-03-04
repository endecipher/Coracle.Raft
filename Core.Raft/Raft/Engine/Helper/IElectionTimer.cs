using System;
using System.Threading;

namespace Coracle.Raft.Engine.Helper
{
    internal interface IElectionTimer : IDisposable
    {
        void RegisterNew(TimerCallback timerCallback);
        void ResetWithDifferentTimeout();
    }
}