using System;
using System.Threading;

namespace EventGuidance.Responsibilities
{
    public interface IActionLock : IDisposable
    {
        void SignalDone();
        void Wait();
        void Wait(System.TimeSpan timeOut, CancellationToken cancellationToken);
    }

}