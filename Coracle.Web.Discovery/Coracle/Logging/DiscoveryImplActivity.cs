using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using EventGuidance.Dependency;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace Coracle.Web.Discovery.Coracle.Logging
{
    public class DiscoveryImplActivity : Activity
    {
        public override Activity WithCallerInfo([CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Context = new DiscoveryImplActivityContext();
            Context.SystemId = nameof(Discovery);
            Context.CallerMethod = memberName;
            Context.CallerLineNumber = sourceLineNumber;
            Context.CallerFilePath = sourceFilePath;

            return this;
        }
    }

    public class DiscoveryImplActivityContext : IActivityContext
    {
        //public DiscoveryImplActivityContext(ICorrelationContextAccessor activityCorrelationIdAccessor)
        //{
        //    CorrelationId = activityCorrelationIdAccessor?.CorrelationContext?.CorrelationId;
        //}

        public string SystemId { get; set; }
        public string CallerFilePath { get; set; }
        public string CallerMethod { get; set; }
        public int CallerLineNumber { get; set; }
        public string CorrelationId { get; set; }
    }
}
