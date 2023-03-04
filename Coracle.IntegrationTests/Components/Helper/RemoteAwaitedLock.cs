using System.Collections.Concurrent;


namespace Coracle.IntegrationTests.Components.Helper
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
