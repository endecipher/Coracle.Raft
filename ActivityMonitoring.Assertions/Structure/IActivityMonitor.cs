
namespace ActivityMonitoring.Assertions.Core
{
    public interface IActivityMonitor<TData>
    {
        void Add(TData item);
        ContextQueueBuilder<TData> Capture();
        void Clear();
        void ClearBackgroundQueue();
        IAssertableQueue<TData> EndCapture(Guid contextQueueId);
        void RemoveContextQueue(IAssertableQueue<TData> contextQueue);
        void RemoveContextQueue(Guid contextQueueId);
        void Start();
        void Stop();
    }
}