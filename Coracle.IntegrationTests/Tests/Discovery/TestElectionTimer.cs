using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;


namespace Coracle.IntegrationTests.Framework
{
    internal class TestElectionTimer : IElectionTimer
    {
        IEngineConfiguration Config { get; }

        public Timer Timer = null;

        public TestElectionTimer(IEngineConfiguration engineConfiguration)
        {
            Config = engineConfiguration;
        }

        /// <summary>
        /// Raft uses randomized election timeouts to ensure that
        /// split votes are rare and that they are resolved quickly.To
        /// prevent split votes in the first place, election timeouts are
        /// chosen randomly from a fixed interval (e.g., 150–300ms).
        /// 
        /// <see cref="Section 5.2 Leader Election"/>
        /// </summary>
        public void ResetWithDifferentTimeout()
        {
            var timeOutInMilliseconds = Convert.ToInt32(RandomTimeout.TotalMilliseconds);

            Timer.Change(timeOutInMilliseconds, timeOutInMilliseconds);
        }

        public TimeSpan RandomTimeout => TimeSpan.FromMilliseconds(new Random()
            .Next(Config.MinElectionTimeout_InMilliseconds, Config.MaxElectionTimeout_InMilliseconds));

        public void Dispose()
        {
            Timer.Dispose();
        }

        public AwaitedLock AwaitedLock { get; set; } = new AwaitedLock();

        public IElectionTimer RegisterNew(TimerCallback timerCallback)
        {
            var timeOutInMilliseconds = Convert.ToInt32(RandomTimeout.TotalMilliseconds);

            var waitedCallback = new TimerCallback((state) =>
            {
                if (AwaitedLock.IsApproved())
                    timerCallback.Invoke(state);
            });

            Timer = new Timer(waitedCallback, null, timeOutInMilliseconds, timeOutInMilliseconds);

            return this;
        }
    }
}
