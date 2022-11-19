namespace EventGuidance.Logging
{
    public interface IEventProcessorConfiguration
    {
        /// <summary>
        /// Event Processor Action Holder Size.
        /// Default is 100 if not specified
        /// </summary>
        public int EventProcessorQueueSize { get; }

        /// <summary>
        /// Control the Aggression of checking new Actions to process if Queue is found empty.
        /// Default is 1000ms / 1s if not specified
        /// </summary>
        public int EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; }
    }
}
