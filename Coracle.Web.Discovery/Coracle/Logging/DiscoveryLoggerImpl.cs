using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ActivityLogger.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace Coracle.Web.Discovery.Coracle.Logging
{
    public class DiscoveryLoggerImpl : IActivityLogger
    {
        public DiscoveryLoggerImpl(IOptions<DiscoveryLoggerOptions> options)
        {
            LoggerOptions = options;
        }

        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Verbose;

        internal DateTimeOffset Now => DateTimeOffset.UtcNow;

        public IOptions<DiscoveryLoggerOptions> LoggerOptions { get; }

        public void Log(ActivityLogger.Logging.Activity e)
        {
            if (!CanProceed(e.Level)) return;

            string message = Newtonsoft.Json.JsonConvert.SerializeObject(e, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Debug.WriteLine(message);
            //LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);

            if (LoggerOptions.Value.ConfigureHandler)
                LoggerOptions.Value.HandlerAction.Invoke(e);
        }

        public bool CanProceed(ActivityLogLevel level)
        {
            return (int)level >= (int)Level;
        }
    }
}
