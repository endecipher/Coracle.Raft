using ActivityLogger.Logging;
using EntityMonitoring.FluentAssertions.Structure;

namespace Coracle.IntegrationTests.Components.Logging
{
    public class TestActivityLogger : IActivityLogger
    {
        public TestActivityLogger(IActivityMonitor<Activity> activityMonitor)
        {
            ActivityMonitor = activityMonitor;
        }

        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Verbose;

        public IActivityMonitor<Activity> ActivityMonitor { get; }

        public void Log(Activity e)
        {
            if (!CanProceed(e.Level)) return;

            ActivityMonitor.Add(e);
        }

        public bool CanProceed(ActivityLogLevel level)
        {
            return (int)level >= (int)Level;
        }
    }
}
