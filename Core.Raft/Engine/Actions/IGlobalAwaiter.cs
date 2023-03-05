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