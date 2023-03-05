using ActivityLogger.Logging;
using Coracle.Raft.Engine.ActivityLogger;
using System.Threading;
using Coracle.Raft.Engine.Node;

namespace Coracle.Raft.Engine.Helper
{
    internal sealed class HeartbeatTimer : IHeartbeatTimer
    {
        #region Constants

        public const string InitiatingCallback = nameof(InitiatingCallback);
        public const string HeartbeatTimerEntity = nameof(HeartbeatTimer);
        public const string TimeOut = nameof(TimeOut);

        #endregion

        public HeartbeatTimer(IEngineConfiguration engineConfiguration, IActivityLogger activityLogger)
        {
            EngineConfiguration = engineConfiguration;
            ActivityLogger = activityLogger;
        }

        IEngineConfiguration EngineConfiguration { get; }
        IActivityLogger ActivityLogger { get; }
        public int TimeOutInMilliseconds => EngineConfiguration.HeartbeatInterval_InMilliseconds;

        public Timer Timer = null;

        public void RegisterNew(TimerCallback timerCallback)
        {
            Timer = new Timer((state) =>
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = HeartbeatTimerEntity,
                    Event = InitiatingCallback,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(TimeOut, TimeOutInMilliseconds))
                .WithCallerInfo());

                timerCallback.Invoke(state);

            }, null, TimeOutInMilliseconds, TimeOutInMilliseconds);
        }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
