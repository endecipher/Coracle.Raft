namespace ActivityMonitoring.Assertions.Core
{
    public interface IActivityMonitorSettings
    {
        bool AcceptWithoutActiveContextQueue { get; }
        bool AutoStart { get; }
        bool ThrowExceptionIfNotStarted { get; }
        int MaximumContextQueueInstances { get; }
    }
}
