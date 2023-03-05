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

using Coracle.Raft.Engine.Configuration.Cluster;
using System;

namespace Coracle.Raft.Engine.Logs
{
    public sealed class LogEntry
    {
        #region Use-case based
        public object Content { get; set; }

        #endregion

        public long Term { get; init; }
        public long CurrentIndex { get; set; }
        public Types Type { get; set; }

        [Flags]
        public enum Types
        {
            /// <summary>
            /// Usually used as a starting/first and only entry across all nodes
            /// </summary>
            None = 1,

            /// <summary>
            /// Signifies Contents as Empty. Used during No-Operation Log Entries for whenever a <see cref="States.Leader"/> is established
            /// </summary>
            NoOperation = 2,

            /// <summary>
            /// Signifies that the underlying Contents denote a <see cref="Command.ICommand"/> to be executed
            /// </summary>
            Command = 4,

            /// <summary>
            /// Signifies that the underlying Contents are actually an enumerable of <see cref="NodeConfiguration"/> 
            /// </summary>
            Configuration = 8,

            /// <summary>
            /// Signifies that the underlying Contents are actually that of a snapshot, and that the logEntry's Index and Term indicate the <c>LastIncludedIndex</c> and <c>LastIncludedTerm</c>
            /// </summary>
            Snapshot = 16
        }
    }
}
