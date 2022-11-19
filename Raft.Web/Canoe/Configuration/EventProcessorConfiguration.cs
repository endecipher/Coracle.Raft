using EventGuidance.Logging;

namespace Raft.Web.Canoe.Configuration
{
    public class EventProcessorConfiguration : IEventProcessorConfiguration
    {
        public int? HoldableSize => 101;

        public int? MillisecondsToWaitIfQueueEmpty => 1001;
    }
}
