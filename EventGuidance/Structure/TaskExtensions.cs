using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventGuidance.Structure
{
    public static class TaskExtensions
    {
        public static async Task<TResult> WithTimeOut<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken? cancellationToken)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cancellationToken ?? CancellationToken.None)))
            {
                return await task;
            }

            throw new TimeoutException();
        }
    }
}
