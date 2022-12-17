namespace Coracle.IntegrationTests.Framework
{
    public class AwaitedLock
    {
        protected object _lock = new object();
        bool KeepApproving { get; set; } = false;

        public AutoResetEvent AutoResetEvent = new AutoResetEvent(false);

        //public IActionLock ContinueTesting = new ActionLock(canReset: true);

        public void ApproveNext()
        {
            lock (_lock)
            {
                KeepApproving = true;
            }

            AutoResetEvent.WaitOne();
        }

        public bool IsApproved()
        {
            lock (_lock) {

                if (KeepApproving)
                {
                    KeepApproving = false;
                    AutoResetEvent.Set();
                    return true;
                }

                return false; 
            }
        }
    }
}
