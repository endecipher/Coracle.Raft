namespace Coracle.Raft.Tests.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OrderAttribute : Attribute
    {
        public OrderAttribute(int Value)
        {
            this.Value = Value;
        }

        public int Value { get; }
    }
}
