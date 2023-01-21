using System;
using System.Threading;

namespace Coracle.Raft.Engine.Helper
{
    internal interface IElectionTimer : IDisposable
    {
        //TODO: why return ElectionTimer?
        IElectionTimer RegisterNew(TimerCallback timerCallback);
        void ResetWithDifferentTimeout();
    }
}