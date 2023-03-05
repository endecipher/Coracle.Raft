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

using System.Collections.Concurrent;


namespace Coracle.Raft.Tests.Components.Helper
{
    /// <summary>
    /// Ok, so the thing is, there will be parallel requests from TestRemoteManager for each MockNode.
    /// Once they reach that point, what we can do is, we can Wait() on a lock. Once main test thread Sets(), we reset().
    /// But, the main testing thread should be allowed to program responses. Therefore, once wait() completes, and we reset(), we call GetResponse(), passing the Input.
    /// The main testing thread can supply a list of operations in terms of funcs, and as the GetResponse is called, we can dequeue and execute.
    /// 
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public class RemoteAwaitedLock<TInput, TResponse> where TInput : class where TResponse : class
    {
        public ConcurrentQueue<(Func<TInput, object> program, ManualResetEventSlim approvalLock)> Programs { get; set; } = new ConcurrentQueue<(Func<TInput, object> program, ManualResetEventSlim approvalLock)>();
        public ConcurrentQueue<(TInput input, object response)> RemoteCalls { get; set; } = new ConcurrentQueue<(TInput input, object response)>();

        public ManualResetEventSlim Enqueue(Func<TInput, object> func)
        {
            var @lock = new ManualResetEventSlim(false);
            Programs.Enqueue((func, @lock));
            return @lock;
        }

        public void ApproveNextInLine()
        {
            foreach (var (program, approvalLock) in Programs)
            {
                if (approvalLock.IsSet)
                    continue;

                approvalLock.Set();
                break;
            }
        }

        public (TResponse response, Exception exception) WaitUntilResponse(TInput input)
        {
            if (Programs.TryDequeue(out var action))
            {
                action.approvalLock.Wait();
                action.approvalLock.Reset();

                var result = action.program.Invoke(input);

                RemoteCalls.Enqueue((input, result));

                if (result is Exception)
                {
                    return (null, (Exception)result);
                }
                else
                {
                    return ((TResponse)result, null);
                }
            }
            else
            {
                Task.Delay(4000).Wait();
            }
            throw new InvalidOperationException($"No Programs Enqueued for {typeof(TInput).AssemblyQualifiedName} and {typeof(TResponse).AssemblyQualifiedName}");
        }
    }
}
