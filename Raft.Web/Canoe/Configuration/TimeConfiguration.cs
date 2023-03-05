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

using Core.Raft.Canoe.Engine.Configuration.Cluster;

namespace Raft.Web.Canoe.Configuration
{
    public sealed class TimeConfiguration : ITimeConfiguration
    {
        public const int Seconds = 1000;

        public const int Minutes = 1000 * 60;

        public int MaxElectionTimeout_InMilliseconds { get; set; } = 10 * Minutes;

        public int MinElectionTimeout_InMilliseconds { get; set; } = 9 * Minutes;

        public int HeartbeatInterval_InMilliseconds { get; set; } = 20 * Minutes;

        public int ClientCommandRetryDelay_InMilliseconds { get; set; } = 50 * Seconds;

        public int ClientCommandTimeout_InMilliseconds { get; set; } = 10 * Minutes;

        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; set; } = 20 * Minutes;

        public int RequestVoteTimeoutOnReceive_InMilliseconds { get; set; } = 20 * Minutes;

        public int RequestVoteTimeoutOnSend_InMilliseconds { get; set; } = 20 * Minutes;

        public int AppendEntriesTimeoutOnSend_InMilliseconds { get; set; } = 20 * Minutes;
    }
}
