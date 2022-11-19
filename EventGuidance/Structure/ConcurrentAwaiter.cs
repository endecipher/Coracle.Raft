using EventGuidance.Responsibilities;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EventGuidance.Structure
{
    public class ConcurrentAwaiter : IConcurrentAwaiter
    {
        ConcurrentQueue<IActionLock> WaitingLocks = new ConcurrentQueue<IActionLock>();

        public void Wait()
        {
            var threadLock = new ActionLock();
            WaitingLocks.Enqueue(threadLock);
            threadLock.Wait();
        }

        public void Wait(System.TimeSpan timeOut, CancellationToken cancellationToken)
        {
            var threadLock = new ActionLock();

            try
            {
                WaitingLocks.Enqueue(threadLock);
                threadLock.Wait(timeOut, cancellationToken);
            }
            catch (Exception)
            {
                threadLock.Dispose();
                throw;
            }
        }

        public void SignalDone()
        {
            while (WaitingLocks.TryDequeue(out var threadLock))
            {
                threadLock.SignalDone();
                threadLock.Dispose();
            }
        }
    }
}
