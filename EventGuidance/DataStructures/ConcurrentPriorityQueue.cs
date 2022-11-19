using System.Collections.Generic;

namespace EventGuidance.DataStructures
{
    public class ConcurrentPriorityQueue<TEntity, TPriorityValue> where TPriorityValue : struct
    {
        private PriorityQueue<TEntity, TPriorityValue> Queue;

        private object _lock = new object();

        public ConcurrentPriorityQueue(int size, IComparer<TPriorityValue> comparer)
        {
            Queue = new PriorityQueue<TEntity, TPriorityValue>(size, comparer);
        }

        public void Enqueue(TEntity entity, TPriorityValue priorityValue)
        {
            lock (_lock)
            {
                Queue.Enqueue(entity, priorityValue);
            }
        }

        public bool TryDequeue(out TEntity entity, out TPriorityValue priorityValue)
        {
            lock (_lock)
            {
                bool hasItem = Queue.TryDequeue(out TEntity obj, out TPriorityValue prio);
                entity = obj;
                priorityValue = prio;
                return hasItem;
            }
        }
    }
}
