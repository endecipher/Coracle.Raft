using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System.Threading;

namespace Core.Raft.Canoe.Engine.Helper
{
    internal sealed class HeartbeatTimer : IHeartbeatTimer
    {
        #region Constants

        public const string InitiatingCallback = nameof(InitiatingCallback);
        public const string Disposing = nameof(Disposing);
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
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = HeartbeatTimerEntity,
                Event = Disposing,
                Level = ActivityLogLevel.Verbose,

            }
            .WithCallerInfo());

            Timer.Dispose();
        }
    }
}
