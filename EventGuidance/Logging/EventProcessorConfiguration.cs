namespace EventGuidance.Logging
{
    public class EventProcessorConfiguration : IEventProcessorConfiguration
    {
        public int EventProcessorQueueSize { get; set; } = 100;

        public int EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; set; } = 1000;
    }
}
