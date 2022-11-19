using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;
using System.Threading;

namespace Core.Raft.Canoe.Engine.Helper
{
    internal class ElectionTimer : IElectionTimer
    {
        IEngineConfiguration Config { get; }

        public Timer Timer = null;

        public ElectionTimer(IEngineConfiguration engineConfiguration)
        {
            Config = engineConfiguration;
        }

        public virtual IElectionTimer RegisterNew(TimerCallback timerCallback)
        {
            var timeOutInMilliseconds = Convert.ToInt32(RandomTimeout.TotalMilliseconds);

            Timer = new Timer(timerCallback, null, timeOutInMilliseconds, timeOutInMilliseconds);

            return this;
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
    }
}
