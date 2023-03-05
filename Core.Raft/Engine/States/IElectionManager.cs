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