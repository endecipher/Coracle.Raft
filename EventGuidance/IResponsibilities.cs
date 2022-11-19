using EventGuidance.Structure;
using System.Collections.Generic;
using System.Threading;

namespace EventGuidance.Responsibilities
{
    public interface IResponsibilities
    {
        string SystemId { get; }
        void ConfigureNew(HashSet<string> invokableActionNames = null, string assigneeId = null);
        TOutput QueueBlockingEventAction<TOutput>(IEventAction action, bool executeSeparately = false) where TOutput : class;
        void QueueEventAction(IEventAction action, bool executeSeparately = false);
        void QueueEventLoopingAction(IEventAction action, bool isSeparate);
        CancellationToken GlobalCancellationToken { get; }
    }
}
