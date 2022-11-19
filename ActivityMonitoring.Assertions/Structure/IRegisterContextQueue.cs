namespace ActivityMonitoring.Assertions.Core
{
    internal interface IRegisterContextQueue<TData>
    {
        IActivityMonitorSettings Settings { get; }
        void Register(IAssertableQueue<TData> queue);
    }
}
