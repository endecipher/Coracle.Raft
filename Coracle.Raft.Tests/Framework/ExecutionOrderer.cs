#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Coracle.Raft.Tests.Framework
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
                        .GetCustomAttributes(typeof(OrderAttribute).AssemblyQualifiedName)
                        .FirstOrDefault()
                        .GetNamedArgument<int>(nameof(OrderAttribute.Value));

                AddTestCase(testCase, order);
            }

            return orderMap.Values.SelectMany(x => x);
        }
    }
}
