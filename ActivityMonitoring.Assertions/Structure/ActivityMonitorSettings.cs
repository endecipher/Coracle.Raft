namespace ActivityMonitoring.Assertions.Core
{
    public class ActivityMonitorSettings : IActivityMonitorSettings
    {
        /// <summary>
        /// If <see cref="AcceptWithoutActiveContextQueue"/> is <c>true</c>, then denotes the maximum <c>TData</c> objects held by the background queue in <see cref="ActivityMonitor{TData}"/>.
        /// <para>Default is <c>unlimited</c></para>
        /// </summary>
        public int MaximumContextQueueInstances { get; init; }

        /// <summary>
        /// If <see cref="AcceptWithoutActiveContextQueue"/> is <c>true</c>, then denotes the maximum <c>TData</c> objects held by the background queue in <see cref="ActivityMonitor{TData}"/>.
        /// <para>Default is <c>unlimited</c></para>
        /// </summary>
        public bool AcceptWithoutActiveContextQueue { get; init; } = true;
        public bool AutoStart { get; init; } = false;

        /// <summary>
        /// Throws Exception if attempting to Add items without the Monitor being started
        /// </summary>
        public bool ThrowExceptionIfNotStarted { get; } = false;
    }
}
