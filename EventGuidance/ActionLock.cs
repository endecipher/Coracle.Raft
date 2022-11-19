using System.Threading;

namespace EventGuidance.Responsibilities
{
    /// <summary>
    /// Singular Thread WaitHandle
    /// This EventLock can be reached only by 2 participants. One, being the waited calling task for the result, and the other, the EventProcessor started computation to set the result. 
    /// Since, only one thread will wait on this, we can afford to not <see cref="ManualResetEventSlim.Reset"/> immediately, post <see cref="ManualResetEventSlim.Set"/>.
    /// Also, we can dispose once the caller waiting task has gone ahead, since we create EventLocks 1:1 per <see cref="Structure.IEventAction"/>
    /// <para/>
    /// <c><see cref="ManualResetEventSlim.Reset"/> will NOT be called - once the doors are open, they remain open. Thus they should be disposed as sson as the necessities are done</c>
    /// </summary>
    public class ActionLock : IActionLock
    {
        public ActionLock(bool canReset = false)
        {
            CanReset = canReset;
        }

        ManualResetEventSlim Lock = new ManualResetEventSlim(false);

        public bool CanReset { get; }

        public void Dispose()
        {
            Lock.Dispose();
            Lock = null;
        }

        public void SignalDone()
        {
            Lock?.Set();

            if (CanReset) 
                Lock?.Reset();
        }

        public void Wait(System.TimeSpan timeOut, CancellationToken cancellationToken)
        {
            Lock?.Wait(timeOut, cancellationToken);
        }

        /// <summary>
        /// Wait without taking any notice for Cancellation or Timeouts.
        /// </summary>
        public void Wait()
        {
            Lock?.Wait();
        }
    }
}
