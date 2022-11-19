using System.Collections.Generic;

namespace EventGuidance.DataStructures
{
    public class RecentPropertyCache<TItem, TProperty> where TItem : struct where TProperty : class
    {
        private Dictionary<TItem, TProperty> Map { get; set; }
        private Queue<TItem> Queue { get; set; }
        public int Capacity { get; }

        public RecentPropertyCache(int capacity)
        {
            Map = new Dictionary<TItem, TProperty>(capacity: capacity);
            Queue = new Queue<TItem>(capacity: capacity);
            Capacity = capacity;
        }

        public bool TryGetItem(TItem item, out TProperty property)
        {
            lock (Map)
            {
                bool hasValue = Map.TryGetValue(item, out var property1);
                property = property1;
                return hasValue;
            }
        }

        public bool TryAddItem(TItem item, TProperty property)
        {
            lock (Map)
            {
                if (Map.ContainsKey(item))
                    return false;

                Map.Add(item, property);
            }

            lock (Queue)
            {
                if (Queue.Count == Capacity) 
                {
                    Queue.Dequeue();
                }

                Queue.Enqueue(item);
            }

            return true;
        }
    }
}
