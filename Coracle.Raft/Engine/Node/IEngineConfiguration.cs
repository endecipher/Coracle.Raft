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
using Coracle.Raft.Engine.Discovery;
using System;

namespace Coracle.Raft.Engine.Node
{
    /// <summary>
    /// Engine Configuration for a Coracle Node - to be supplied during initialization
    /// 
    /// </summary>
    /// <remarks>
    /// Leader election is the aspect of Raft where timing is most critical.Raft will be able to elect and maintain a steady leader as long 
    /// as the system satisfies the following timing requirement:
    /// 
    /// broadcastTime « electionTimeout « MTBF
    /// 
    /// In this inequality broadcastTime is the average time it takes a server to send RPCs in parallel to every server
    /// in the cluster and receive their responses; electionTimeout is the election timeout described in Section 5.2; and
    /// MTBF is the average time between failures for a single server.
    /// 
    /// The broadcast time and MTBF are properties of the underlying system, while the election timeout is something we must choose.
    /// Raft’s RPCs typically require the recipient to persist information to stable storage, so the broadcast time may range from 0.5ms to 20ms, depending on
    /// storage technology. As a result, the election timeout is likely to be somewhere between 10ms and 500ms. Typical server MTBFs are several 
    /// months or more, which easily satisfies the timing requirement.
    /// 
    /// <seealso cref="5.6 Timing and availability"/>
    /// </remarks>
    public interface IEngineConfiguration
    {
        #region Event Processor Settings

        /// <summary>
        /// Event Processor Action Queue Size
        /// </summary>
        public int ProcessorQueueSize { get; }

        /// <summary>
        /// Control the Aggression of checking new Actions to process if Queue is found empty.
        /// </summary>
        public int ProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; }

        #endregion

        #region Coracle Node Settings
        public Uri NodeUri { get; }

        public string NodeId { get; }

        #endregion

        #region Coracle Other Settings

        public bool IncludeOriginalClientCommandInResults { get; }
        public bool IncludeOriginalConfigurationInResults { get; }
        public bool IncludeJointConsensusConfigurationInResults { get; }
        public bool IncludeConfigurationChangeRequestInResults { get; }

        /// <summary>
        /// Determines the minimum count of log entries required for snapshot compaction to trigger and for a valid merge.
        /// If an existing snapshot is already present, the threshold would include it as a single entry during eligibility computation. 
        /// Acceptable value is a positive integer greater than or equal to 5
        /// </summary>
        public int SnapshotThresholdSize { get; }

        /// <summary>
        /// Determines the minimum count of additional log entries to be excluded from compaction. 
        /// This serves as a safe buffer distance from the last log entry applied during snapshot compaction, 
        /// so that eligibility fails if the Snapshot to create is dangerously close to the last log entry.
        /// Acceptable value is a positive integer greater than or equal to 1
        /// </summary>
        public int SnapshotBufferSizeFromLastEntry { get; }

        #endregion

        #region Coracle Time Settings

        /// <summary>
        /// Time in Milliseconds that this Coracle Node would wait post <see cref="IDiscoveryHandler.EnrollThisNode(Uri, INodeConfiguration, System.Threading.CancellationToken)"/> during <see cref="ICoracleNode.Start"/>
        /// </summary>
        public int WaitPostEnroll_InMilliseconds { get; }

        /// <summary>
        /// Max Election Timeout in Milliseconds. Used together with <see cref="MinElectionTimeout_InMilliseconds"/> to decide on a random timeout for Election Candidacy.
        /// </summary>
        public int MaxElectionTimeout_InMilliseconds { get; }

        /// <summary>
        /// Min Election Timeout in Milliseconds. Used together with <see cref="MaxElectionTimeout_InMilliseconds"/> to decide on a random timeout for Election Candidacy
        /// </summary>
        public int MinElectionTimeout_InMilliseconds { get; }

        /// <summary>
        /// Heartbeat Interval in Milliseconds. Used for periodic sending out of Heartbeat AppendEntries RPCs to all Peer Nodes when System State is Leader
        /// </summary>
        public int HeartbeatInterval_InMilliseconds { get; }

        /// <summary>
        /// Time Interval in Milliseconds to determine how aggressively the node should check whether a Leader Node is elected for the cluster system.
        /// Threads processing Client Commands will have to until a Leader Node Configuration is updated via an External Append Entries RPC
        /// </summary>
        public int NoLeaderElectedWaitInterval_InMilliseconds { get; }

        /// <summary>
        /// Client Command Timeout In Milliseconds
        /// </summary>
        public int ClientCommandTimeout_InMilliseconds { get; }

        /// <summary>
        /// Configuration Change Handle Timeout In Milliseconds
        /// </summary>
        public int ConfigurationChangeHandleTimeout_InMilliseconds { get; }

        /// <summary>
        /// Response Timeout In Milliseconds for an outgoing Append Entries RPC
        /// </summary>
        public int AppendEntriesTimeoutOnSend_InMilliseconds { get; }

        /// <summary>
        /// Processing Timeout In Milliseconds when received an external Append Entries RPC
        /// </summary>
        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; }

        /// <summary>
        /// Response Timeout In Milliseconds for an outgoing Request Vote RPC
        /// </summary>
        public int RequestVoteTimeoutOnSend_InMilliseconds { get; }

        /// <summary>
        /// Processing Timeout In Milliseconds when received an external Request Vote RPC
        /// </summary>
        public int RequestVoteTimeoutOnReceive_InMilliseconds { get; }

        /// <summary>
        /// This determines how long to wait for an entry to be committed, i.e successful replication across a majority of clusters, 
        /// so that the leader can commit the log entry.
        /// 
        /// Both non-Readonly commands, and leader decommission on configuration change would want to wait for a log Entry to be committed before responding, or decommission respectively.
        /// 
        /// It can be equal to the ElectionTimeout only, since we can wait until that long.
        /// </summary>
        public int EntryCommitWaitTimeout_InMilliseconds { get; }

        /// <summary>
        /// This determines how aggressively to check for an entry to be committed, i.e successful replication across a majority of clusters, 
        /// so that the leader can commit the log entry.
        /// 
        /// Both non-Readonly commands, and leader decommission on configuration change would want to wait for a log Entry to be committed before responding, or decommission respectively.
        /// 
        /// It can be a small fraction of the Heartbeat timeout, or ideally - the amount of time o send and receive an RPC in average.
        /// Consider an average network call time.
        /// </summary>
        public int EntryCommitWaitInterval_InMilliseconds { get; }

        /// <summary>
        /// The catchup of NewNodes may take a long time, since they have newly joined.
        /// If InstallSnapshotRPC is not configured, the traditional AppendEntries RPC may take a longer while.
        /// For the worst case, we can keep it as a very large value
        /// </summary>
        public int CatchUpOfNewNodesTimeout_InMilliseconds { get; }

        /// <summary>
        /// Determines how aggressively to check if the New Nodes are caught up or not.
        /// 
        /// The catchup of NewNodes may take a long time, since they have newly joined.
        /// If InstallSnapshotRPC is not configured, the traditional AppendEntries RPC may take a longer while.
        /// 
        /// We can keep it as the same time as Heartbeat Interval for now.
        /// </summary>
        public int CatchUpOfNewNodesWaitInterval_InMilliseconds { get; }

        /// <summary>
        /// For responding to Read-only requests, the leader has to check if it has been deposed or not. This controls the aggression. 
        /// Setting it to a lower value than heartbeat will help in faster responses.
        /// </summary>
        public int CheckDepositionWaitInterval_InMilliseconds { get; }

        /// <summary>
        /// Processing Timeout In Milliseconds when sending an Install Snapshot RPC for a chunk of data
        /// </summary>
        public int InstallSnapshotChunkTimeoutOnSend_InMilliseconds { get; }

        /// <summary>
        /// Processing Timeout In Milliseconds when received an external Install Snapshot RPC for a chunk of data
        /// </summary>
        public int InstallSnapshotChunkTimeoutOnReceive_InMilliseconds { get; }

        /// <summary>
        /// Processing Timeout for the entire compaction process; from eligibility to the actual snapshot creation and merging, if it occurs.
        /// Setting to a large/the maximum value is recommended.
        /// </summary>
        public int CompactionAttemptTimeout_InMilliseconds { get; }

        /// <summary>
        /// Within the Compaction Process, determines how aggressively the eligibility for compaction should occur.
        /// </summary>
        public int CompactionAttemptInterval_InMilliseconds { get; }

        /// <summary>
        /// Within the Compaction Process, controls how stale the current committed snapshot's last used time should be for the eligibility for re-compaction.
        /// Setting it to a smaller value is not recommended, since the current snapshot might still be in use for installation.
        /// </summary>
        public int CompactionWaitPeriod_InMilliseconds { get; }

        #endregion
    }
}



