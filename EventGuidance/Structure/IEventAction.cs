using EventGuidance.Cancellation;
using System;

namespace EventGuidance.Structure
{
    public interface IEventAction
    {
        ICancellationManager CancellationManager { get; }

        ActionPriorityValues PriorityValue { get; }

        TimeSpan TimeOut { get; }

        string UniqueName { get; }
    }
}
