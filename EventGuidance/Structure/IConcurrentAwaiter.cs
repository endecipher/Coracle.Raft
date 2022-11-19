using System;
using System.Threading;

namespace EventGuidance.Structure
{
    public interface IConcurrentAwaiter
    {
        void SignalDone();
        void Wait();
        void Wait(TimeSpan timeOut, CancellationToken cancellationToken);
    }
}