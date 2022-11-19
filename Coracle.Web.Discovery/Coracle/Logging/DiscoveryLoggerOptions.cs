using ActivityLogger.Logging;

namespace Coracle.Web.Discovery.Coracle.Logging
{
    public class DiscoveryLoggerOptions
    {
        public bool ConfigureHandler { get; set; } = false;
        public Action<Activity> HandlerAction { get; set; }
    }
}
