using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace Coracle.Web.Discovery.Coracle.Logging
{
    public class DiscoveryLoggerImpl : IActivityLogger
    {
        public DiscoveryLoggerImpl(ICorrelationContextAccessor  correlationContext, IOptions<DiscoveryLoggerOptions> options)
        {
            CorrelationContextAccessor = correlationContext;
            LoggerOptions = options;
        }

        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Verbose;

        internal DateTimeOffset Now => DateTimeOffset.UtcNow;

        public ICorrelationContextAccessor CorrelationContextAccessor { get; }
        public IOptions<DiscoveryLoggerOptions> LoggerOptions { get; }

        public void Log(ActivityLogger.Logging.Activity e)
        {
            if (!CanProceed(e.Level)) return;

            var activity = new
            {
                CorrelationId = CorrelationContextAccessor?.CorrelationContext?.CorrelationId,
                Activity = e
            };

            string message = Newtonsoft.Json.JsonConvert.SerializeObject(activity, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
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
