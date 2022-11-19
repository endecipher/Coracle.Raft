using System;
using System.Threading;

namespace Core.Raft.Canoe.Engine.Helper
{
    internal interface IElectionTimer : IDisposable
    {
        //TODO: why return ElectionTimer?
        IElectionTimer RegisterNew(TimerCallback timerCallback);
        void ResetWithDifferentTimeout();
    }
}