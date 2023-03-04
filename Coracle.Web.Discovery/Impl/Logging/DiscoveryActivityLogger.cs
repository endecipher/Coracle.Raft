using System.Diagnostics;
using ActivityLogger.Logging;
using Newtonsoft.Json;
namespace Coracle.Web.Discovery.Coracle.Logging
{
    public class DiscoveryActivityLogger : IActivityLogger
    {
        public DiscoveryActivityLogger()
        {
        }

        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Verbose;

        internal DateTimeOffset Now => DateTimeOffset.UtcNow;

        public void Log(ActivityLogger.Logging.Activity e)
        {
            if (!CanProceed(e.Level)) return;

            string message = JsonConvert.SerializeObject(e, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Debug.WriteLine(message);
        }

        public bool CanProceed(ActivityLogLevel level)
        {
            return (int)level >= (int)Level;
        }
    }
}
