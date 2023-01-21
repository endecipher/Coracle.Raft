using Microsoft.AspNetCore.SignalR;
using Coracle.Web.Hubs;
using System.Diagnostics;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Coracle.IntegrationTests.Components.PersistentData;

namespace Coracle.Web.Impl.Logging
{
    public class ActivityLoggerOptions
    {
        public bool ConfigureHandler { get; set; } = false;
        public Action<ActivityLogger.Logging.Activity> HandlerAction { get; set; }
    }

    public class WebActivityLogger : IActivityLogger
    {
        public WebActivityLogger(ICorrelationContextAccessor correlationContext, IHubContext<LogHub> logHubContext, IHubContext<RaftHub> raftHubContext, IOptions<ActivityLoggerOptions> options)
        {
            CorrelationContextAccessor = correlationContext;
            LogHubContext = logHubContext;
            RaftHubContext = raftHubContext;
            LoggerOptions = options;
        }

        public IHubContext<LogHub> LogHubContext { get; }
        public IHubContext<RaftHub> RaftHubContext { get; }
        public IOptions<ActivityLoggerOptions> LoggerOptions { get; }
        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Debug;
        public ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public void Log(ActivityLogger.Logging.Activity e)
        {
            if (!CanProceed(e.Level)) return;

            var activity = new
            {
                CorrelationContextAccessor?.CorrelationContext?.CorrelationId,
                Activity = e
            };

            if (e.EntitySubject.Equals(TestStateProperties.Entity))
            {
                var currentTerm = e.Parameters.FirstOrDefault(x => x.Name.Equals(TestStateProperties.CurrentTermValue));
                var votedFor = e.Parameters.FirstOrDefault(x => x.Name.Equals(TestStateProperties.VotedForValue));

                RaftHubContext.Clients.All.SendAsync(RaftHub.ReceiveEntries, $"Term: {currentTerm.Value} | VotedFor: {votedFor.Value}");
            }

            if (e.EntitySubject.Equals(TestLogHolder.Entity))
            {
                var logChain = e.Parameters.FirstOrDefault(x => x.Name.Equals(TestLogHolder.LogChain));

                RaftHubContext.Clients.All.SendAsync(RaftHub.ReceiveEntries, logChain.Value);
            }

            string message = JsonConvert.SerializeObject(activity, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Debug.WriteLine(message);

            LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);

            if (LoggerOptions.Value.ConfigureHandler)
                LoggerOptions.Value.HandlerAction.Invoke(e);
        }

        public bool CanProceed(ActivityLogLevel level)
        {
            return (int)level >= (int)Level;
        }
    }
}
