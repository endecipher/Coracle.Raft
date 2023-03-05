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

namespace Coracle.Raft.Engine.Command
{
    public interface ICommand
    {
        string UniqueId { get; }
        
        bool IsReadOnly { get; }

        string Type { get; }
    }


    /// <remarks>
    /// Our goal for Raft is to implement linearizable semantics (each operation appears to execute instantaneously,
    /// exactly once, at some point between its invocation and
    /// its response). However, as described so far Raft can execute a command multiple times: for example, if the leader
    /// crashes after committing the log entry but before responding to the client, the client will retry the command with a
    /// new leader, causing it to be executed a second time.
    /// 
    /// The solution is for clients to assign unique serial numbers to
    /// every command. Then, the state machine tracks the latest
    /// serial number processed for each client, along with the associated response. If it receives a command whose serial
    /// number has already been executed, it responds immediately without re-executing the request.
    /// <seealso cref="Section 8 Client Interaction"/>
    /// </remarks>

    /// <remarks>
    /// Read-only operations can be handled without writing
    /// anything into the log.However, with no additional measures, this would run the risk of returning stale data, since
    /// the leader responding to the request might have been superseded by a newer leader of which it is unaware.Linearizable reads must not return stale data, and Raft needs
    /// two extra precautions to guarantee this without using the
    /// log.First, a leader must have the latest information on
    /// which entries are committed.The Leader Completeness
    /// Property guarantees that a leader has all committed entries, but at the start of its term, it may not know which
    /// those are.To find out, it needs to commit an entry from
    /// its term.Raft handles this by having each leader commit a blank no-op entry into the log at the start of its
    /// term.Second, a leader must check whether it has been deposed before processing a read-only request (its information may be stale if a more recent leader has been elected).
    /// Raft handles this by having the leader exchange heartbeat messages with a majority of the cluster before responding to read-only requests.
    /// 
    /// <seealso cref="Section 8 Client Interaction"/>
    /// </remarks>
}
