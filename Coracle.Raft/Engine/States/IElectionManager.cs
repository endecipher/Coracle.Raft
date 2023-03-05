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

using Coracle.Raft.Engine.Configuration.Alterations;

namespace Coracle.Raft.Engine.States
{
    /// <remarks>
    /// A candidate wins an election if it receives votes from a majority of the servers in the full cluster for the same term.
    /// Each server will vote for at most one candidate in a given term, on a first - come - first - served basis.
    /// The majority rule ensures that at most one candidate can win the election for a particular term
    /// <see cref="Section 5.2 Leader Election"/>
    /// </remarks>
    internal interface IElectionManager : IMembershipUpdate
    {
        void Initiate(long term);
        bool CanSendTowards(string uniqueNodeId, long term);
        void IssueRetry(string uniqueNodeId);
        void UpdateFor(long term, string uniqueNodeId, bool voteGranted);
        void CancelSessionIfExists();
    }
}
