using Xunit.Abstractions;
using Xunit.Sdk;

namespace Coracle.IntegrationTests.Framework
{
    public class ExecutionOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            var orderMap = new SortedDictionary<int, List<TTestCase>>();

            void AddTestCase(TTestCase testCaseToOrder, int determinedOrder)
            {
                if (orderMap.TryGetValue(determinedOrder, out List<TTestCase> testCases))
                {
                    testCases.Add(testCaseToOrder);
                }
                else
                {
                    orderMap.Add(determinedOrder, new List<TTestCase> { testCaseToOrder });
                }
            }

            foreach (TTestCase testCase in testCases)
            {
                int order =
                    testCase
                        .TestMethod
                        .Method
                        .GetCustomAttributes((typeof(OrderAttribute).AssemblyQualifiedName))
                        .FirstOrDefault()
                        .GetNamedArgument<int>(nameof(OrderAttribute.Value));

                AddTestCase(testCase, order);
            }

            return orderMap.Values.SelectMany(x => x);
        }
    }
}
