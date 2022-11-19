namespace ActivityMonitoring.Assertions.Core
{
    public class ContextQueueBuilder<TData> : IDisposable
    {
        IRegisterContextQueue<TData> Monitor { get; set; }

        internal ContextQueueBuilder(IRegisterContextQueue<TData> monitor)
        {
            Monitor = monitor;
        }

        public INotifiableQueue<TData> All()
        {
            var contextQueueId = Guid.NewGuid();

            var queue = new ContextQueue<TData>(contextQueueId, _ => true);

            Monitor.Register(queue);

            Dispose();

            return queue as INotifiableQueue<TData>;
        }

        public INotifiableQueue<TData> WithCondition(Func<TData, bool> captureCondition)
        {
            var contextQueueId = Guid.NewGuid();

            var queue = new ContextQueue<TData>(contextQueueId, captureCondition);

            Monitor.Register(queue);

            Dispose();

            return queue as INotifiableQueue<TData>;
        }

        public void Dispose()
        {
            Monitor = null;
        }
    }
}
