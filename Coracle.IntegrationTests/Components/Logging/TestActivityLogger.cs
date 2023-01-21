using ActivityLogger.Logging;
using EntityMonitoring.FluentAssertions.Structure;

namespace Coracle.IntegrationTests.Components.Logging
{
    public class TestActivityLogger : IActivityLogger
    {
        public TestActivityLogger(IActivityMonitor<ActivityLogger.Logging.Activity> activityMonitor)
        {
            ActivityMonitor = activityMonitor;
        }

        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Verbose;

        internal DateTimeOffset Now => DateTimeOffset.UtcNow;

        public IActivityMonitor<ActivityLogger.Logging.Activity> ActivityMonitor { get; }

        public void Log(ActivityLogger.Logging.Activity e)
        {
            if (!CanProceed(e.Level)) return;

            ActivityMonitor.Add(e);

            //string message = JsonConvert.SerializeObject(e, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            //Debug.WriteLine(message);
            ////LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);

            //if (LoggerOptions.Value.ConfigureHandler)
            //    LoggerOptions.Value.HandlerAction.Invoke(e);
        }

        public bool CanProceed(ActivityLogLevel level)
        {
            return (int)level >= (int)Level;
        }
    }
}
