namespace ActivityMonitoring.Assertions.Core
{
    internal class ContextQueue<TData> : ActivityQueue<TData>
    {
        protected bool IsContextActive { get; set; }
        Func<TData, bool> CanAppend { get; }

        internal ContextQueue(Guid instanceId, Func<TData, bool> appendCondition) : base(instanceId)
        {
            IsContextActive = true;
            CanAppend = appendCondition;
        }

        internal void DisallowCaptures()
        {
            IsContextActive = false;
        }

        protected internal override void Add(TData item)
        {
            if (!IsContextActive || !CanAppend.Invoke(item))
                return;

            base.Add(item);
        }
    }
}
