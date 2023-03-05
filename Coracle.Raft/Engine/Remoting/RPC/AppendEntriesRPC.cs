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

using Coracle.Raft.Engine.Logs;
using System.Collections.Generic;
using System.Linq;

namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IAppendEntriesRPC : IRemoteCall
    {
        IEnumerable<LogEntry> Entries { get; }
        bool IsHeartbeat { get; }
        long LeaderCommitIndex { get; }
        string LeaderId { get; }
        long PreviousLogIndex { get; }
        long PreviousLogTerm { get; }
        long Term { get; }
    }

    public class AppendEntriesRPC : IAppendEntriesRPC
    {
        public AppendEntriesRPC()
        {

        }

        public AppendEntriesRPC(IEnumerable<LogEntry> entries) : this()
        {
            Entries = entries;
        }

        public bool IsHeartbeat => Entries == null || !Entries.Any();

        public long Term { get; set; }

        public string LeaderId { get; set; }

        public long PreviousLogIndex { get; set; }

        public long PreviousLogTerm { get; set; }

        public IEnumerable<LogEntry> Entries { get; set; } = default;

        public long LeaderCommitIndex { get; set; }
    }
}
