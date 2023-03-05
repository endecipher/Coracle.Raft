namespace Coracle.Raft.Tests.Components.Helper
{
    public class AwaitedLock
    {
        protected object _lock = new object();
        bool KeepApproving { get; set; } = false;

        public AutoResetEvent AutoResetEvent = new AutoResetEvent(false);

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
            lock (_lock)
            {

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
