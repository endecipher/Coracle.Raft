using Microsoft.AspNetCore.SignalR;
using Coracle.Web.Hubs;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace Coracle.Web.Logging
{

    public class ActivityLoggerOptions
    {
        public bool ConfigureHandler { get; set; } = false;
        public Action<ActivityLogger.Logging.Activity> HandlerAction { get; set; }
    }


    public class ActivityLoggerImpl : IActivityLogger
    {
        public ActivityLoggerImpl(ICorrelationContextAccessor  correlationContext, IHubContext<LogHub> logHubContext, IOptions<ActivityLoggerOptions> options)
        {
            CorrelationContextAccessor = correlationContext;
            LogHubContext = logHubContext;
            LoggerOptions = options;
        }

        public IHubContext<LogHub> LogHubContext { get; }
        public IOptions<ActivityLoggerOptions> LoggerOptions { get; }
        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Verbose;

        internal DateTimeOffset Now => DateTimeOffset.UtcNow;

        public ICorrelationContextAccessor CorrelationContextAccessor { get; }

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
