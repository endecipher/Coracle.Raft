using System.Collections.Concurrent;

namespace ActivityMonitoring.Assertions.Core
{
    internal class Notifier<TData> : INotifier<TData>
    {
        public Notifier()
        {
            IsConditionMatched = false;
        }

        public bool IsConditionMatched { get; protected set; }
        public bool RemovePostMatch { get; protected set; } = false;

        public Func<TData, bool> Condition { get; init; }

        AutoResetEvent Awaiter { get; set; } = new AutoResetEvent(false);

        //TODO: Remove CancellationToken from signature
        public void Wait(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            Awaiter?.WaitOne(timeSpan);
        }

        public void Wait()
        {
            Awaiter?.WaitOne();
        }

        public INotifier<TData> RemoveOnceMatched()
        {
            RemovePostMatch = true;
            return this;
        }

        protected internal bool Verify(TData data)
        {
            if (Condition(data))
            {
                Awaiter?.Set();
                IsConditionMatched = true;
                return true;
            }

            return false;
        }

        internal void Reset()
        {
            IsConditionMatched = false;
            Awaiter?.Reset();
        }
    }

    public interface INotifiableQueue<TData>
    {
        Guid Id { get; }
        INotifier<TData> AttachNotifier(Func<TData, bool> notifyingCondition);
    }

    internal class ActivityQueue<TData> : IAssertableQueue<TData>, INotifiableQueue<TData>, IDisposable
    {
        public ConcurrentQueue<TData> Queue { get; } 

        public ConcurrentDictionary<int, Notifier<TData>> Notifiers { get; } 

        public Guid Id { get; }

        internal ActivityQueue(Guid instanceId)
        {
            Id = instanceId;

            Queue = new ConcurrentQueue<TData>();
            Notifiers = new ConcurrentDictionary<int, Notifier<TData>>();
        }

        protected internal void Clear()
        {
            Queue.Clear(); 
        }

        protected internal virtual void Add(TData item)
        {
            Queue.Enqueue(item);

            foreach (var (hashId, notifier) in Notifiers)
            {
                if (notifier.Verify(item))
                {
                    //TODO: Having Reset called immediately won't make sure atleast 1 blocking thread is made to conitnue; That's why not caling resetnotifier.Reset();

                    if (notifier.RemovePostMatch)
                    {
                        Notifiers.TryRemove(hashId, out var removedNotifier);
                    }
                }
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public INotifier<TData> AttachNotifier(Func<TData, bool> notifyingCondition)
        {
            Notifier<TData> notifier = new Notifier<TData>
            {
                Condition = notifyingCondition
            };

            int hashCode = notifier.GetHashCode();

            Notifiers.AddOrUpdate(hashCode, notifier, (hash, n) => n);

            return notifier;
        }
    }
}
