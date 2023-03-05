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

using System.Threading;

namespace Coracle.Raft.Engine.Actions
{
    internal interface IGlobalAwaiter
    {
        /// <summary>
        /// Only applicable if the current Node is <see cref="States.Leader"/>.
        /// Waits for the <see cref="LogEntry"/> with index <paramref name="logEntryIndex"/> to be committed and applied to the state machine
        /// </summary>
        /// <param name="logEntryIndex">Index of the Log Entry</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        void AwaitEntryCommit(long logEntryIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Only applicable if the current Node is <see cref="States.Leader"/>.
        /// Waits until a Majority of the Cluster is hit with outbound AppendEntries (doesn't check whether successful or not). 
        /// This is done to wait for a period while sending out AppendEntries, and check whether the Current Node is still the Leader.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        void AwaitNoDeposition(CancellationToken cancellationToken);

        /// <summary>
        /// Only applicable if the current Node is <see cref="States.Leader"/>
        /// Waits for the Peer Nodes having Server Ids as <paramref name="nodeIdsToCheck"/>, to be caught up. 
        /// The current node would have to replicate log entries upto a certain index <paramref name="logEntryIndex"/>
        /// </summary>
        /// <param name="logEntryIndex">Index of the Log Entry</param>
        /// <param name="nodeIdsToCheck">Peer Node Ids</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        void AwaitNodesToCatchUp(long logEntryIndex, string[] nodeIdsToCheck, CancellationToken cancellationToken);
    }
}
