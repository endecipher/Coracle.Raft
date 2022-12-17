namespace EventGuidance.Structure
{
    public enum EventActionStatusValues
    {
        New = 0,
        Processing = 1,
        Faulted = 3,
        Completed = 2,
        Cancelled = 4,
        TimedOut = 5,
        Stopped = 6,
        Skipped = 7
    }
}
