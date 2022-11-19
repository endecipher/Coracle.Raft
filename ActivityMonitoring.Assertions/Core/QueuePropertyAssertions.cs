using FluentAssertions.Execution;

namespace ActivityMonitoring.Assertions.Core
{
    public class QueuePropertyAssertions<TData>
    {
        internal QueuePropertyAssertions(IAssertableQueue<TData> assertableQueue)
        {
            AssertableQueue = assertableQueue as ActivityQueue<TData>;
        }

        internal ActivityQueue<TData> AssertableQueue { get; }

        public void HavingCount(int expectedCount, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => AssertableQueue.Queue)
                .ForCondition((queue) =>
                {
                    return queue.Count.Equals(expectedCount);
                })
                .FailWith($"{nameof(expectedCount)}:{expectedCount} does not match with actual count {AssertableQueue.Queue.Count}");
        }
    }
}
