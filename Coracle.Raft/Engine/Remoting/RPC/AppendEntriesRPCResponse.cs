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

namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IAppendEntriesRPCResponse : IRemoteResponse
    {
        long? ConflictingEntryTermOnFailure { get; }
        long? FirstIndexOfConflictingEntryTermOnFailure { get; }
        bool Success { get; }
        long Term { get; }
    }

    public class AppendEntriesRPCResponse : IAppendEntriesRPCResponse
    {
        public long Term { get; set; }

        public bool Success { get; set; }

        public long? ConflictingEntryTermOnFailure { get; set; }

        public long? FirstIndexOfConflictingEntryTermOnFailure { get; set; }
    }

    /// <remarks>
    /// The following two additional properties are with respect to the below passage.
    /// 
    /// If desired, the protocol can be optimized to reduce the number of rejected AppendEntries RPCs.
    /// For example,when rejecting an AppendEntries request, the follower can include the term of the conflicting entry and the 
    /// first index it stores for that term. 
    /// 
    /// With this information, the leader can decrement nextIndex to bypass all of the conflicting entries in that term; 
    /// one AppendEntries RPC will be required for each term with conflicting entries, rather than one RPC per entry.
    /// <seealso cref="Section 5.3 Log Replication"/>
    /// </remarks>
}
