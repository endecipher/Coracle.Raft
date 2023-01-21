using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Helper;

namespace Coracle.IntegrationTests.Framework
{
    internal class TestHeartbeatTimer : IHeartbeatTimer
    {
        public TestHeartbeatTimer(IEngineConfiguration engineConfiguration)
        {
            EngineConfiguration = engineConfiguration;
        }

        IEngineConfiguration EngineConfiguration { get; }
        public int TimeOutInMilliseconds => EngineConfiguration.HeartbeatInterval_InMilliseconds;

        public Timer Timer = null;

        public void Dispose()
        {
            Timer.Dispose();
        }

        public AwaitedLock AwaitedLock { get; set; } = new AwaitedLock();

        public void RegisterNew(TimerCallback timerCallback)
        {
            var waitedCallback = new TimerCallback((state) =>
            {
                if (AwaitedLock.IsApproved())
                    timerCallback.Invoke(state);
            });

            Timer = new Timer(waitedCallback, null, TimeOutInMilliseconds, TimeOutInMilliseconds);
        }
    }
}
