using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Configuration;
using CorrelationId.Abstractions;
using EventGuidance.Dependency;
using System.Runtime.CompilerServices;

namespace Coracle.Web.Coracle.Logging
{
    //public class ActivityCorrelationIdAccessor : IActivityCorrelationIdAccessor
    //{
    //    public ActivityCorrelationIdAccessor(ICorrelationContextAccessor correlationContextAccessor)
    //    {
    //        CorrelationContextAccessor = correlationContextAccessor;
    //    }

    //    public string CorrelationId => CorrelationContextAccessor?.CorrelationContext?.CorrelationId;

    //    public ICorrelationContextAccessor CorrelationContextAccessor { get; }
    //}

    public class ImplActivity : Activity
    {
        public override Activity WithCallerInfo([CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Context = ComponentContainer.Instance.GetInstance<IActivityContext>();
            Context.SystemId = ComponentContainer.Instance.GetInstance<IClusterConfiguration>().ThisNode.UniqueNodeId;
            Context.CallerMethod = memberName;
            Context.CallerLineNumber = sourceLineNumber;
            Context.CallerFilePath = sourceFilePath;

            return this;
        }
    }

    public class ImplActivityContext : IActivityContext
    {
        public ImplActivityContext(ICorrelationContextAccessor activityCorrelationIdAccessor)
        {
            CorrelationId = activityCorrelationIdAccessor?.CorrelationContext?.CorrelationId;
        }

        public string SystemId { get; set; }
        public string CallerFilePath { get; set; }
        public string CallerMethod { get; set; }
        public int CallerLineNumber { get; set; }
        public string CorrelationId { get; set; }
    }
}
