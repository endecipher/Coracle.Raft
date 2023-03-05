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

namespace Coracle.Raft.Tests.Components.Helper
{
    public class AwaitedLock
    {
        protected object _lock = new object();
        bool KeepApproving { get; set; } = false;

        public AutoResetEvent AutoResetEvent = new AutoResetEvent(false);

        public void ApproveNext()
        {
            lock (_lock)
            {
                KeepApproving = true;
            }

            AutoResetEvent.WaitOne();
        }

        public bool IsApproved()
        {
            lock (_lock)
            {

                if (KeepApproving)
                {
                    KeepApproving = false;
                    AutoResetEvent.Set();
                    return true;
                }

                return false;
            }
        }
    }
}
