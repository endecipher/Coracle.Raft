using FluentAssertions.Execution;

namespace ActivityMonitoring.Assertions.Core
{
    public class SearchAssertions<TData> 
    {
        internal SearchAssertions(IAssertableQueue<TData> assertableQueue)
        {
            AssertableQueue = assertableQueue as ActivityQueue<TData>;
        }

        internal ActivityQueue<TData> AssertableQueue { get; }

        public void UntilItContains(TData expectedData, IComparer<TData> comparer = null, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => AssertableQueue.Queue)
                .ForCondition((queue) =>
                {
                    bool IsExpectedElementFound = false;

                    foreach (var item in queue)
                    {
                        if (comparer != null)
                        {
                            IsExpectedElementFound = comparer.Compare(expectedData, item) == 0;
                        }
                        else
                        {
                            IsExpectedElementFound = item.Equals(expectedData);
                        }

                        if (IsExpectedElementFound)
                        {
                            break;
                        }
                    }

                    return IsExpectedElementFound;
                })
                .FailWith($"{nameof(expectedData)} not found");
        }

        public void UntilItSatisfies(Func<TData, bool> matchingCondition, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => AssertableQueue.Queue)
                .ForCondition((queue) =>
                {
                    bool IsExpectedElementFound = false;

                    foreach (var item in queue)
                    {
                        IsExpectedElementFound = matchingCondition.Invoke(item);

                        if (IsExpectedElementFound)
                        {
                            break;
                        }
                    }

                    return IsExpectedElementFound;
                })
                .FailWith($"{nameof(matchingCondition)} failed to find any matches");
        }
    }
}
