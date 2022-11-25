﻿using System;

namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    /// <summary>
    /// Engine Configuration for a Coracle Node - to be supplied during initialization
    /// 
    /// </summary>
    /// <remarks>
    /// Leader election is the aspect of Raft where timing is most critical.Raft will be able to elect and maintain a steady leader as long 
    /// as the system satisfies the following timing requirement:
    /// 
    /// broadcastTime ≪ electionTimeout ≪ MTBF
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
        #region Discovery Settings 
        public Uri DiscoveryServerUri { get; }

        #endregion

        #region Event Processor Settings

        /// <summary>
        /// Event Processor Action Queue Size
        /// </summary>
        public int EventProcessorQueueSize { get; }

        /// <summary>
        /// Control the Aggression of checking new Actions to process if Queue is found empty.
        /// </summary>
        public int EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; }

        #endregion

        #region Coracle Node Settings

        public string NodeId { get; }

        public string AppendEntriesEndpoint { get; }

        public string RequestVoteEndpoint { get; }

        public string CommandHandlingEndpoint { get; }

        #endregion

        #region Coracle Other Settings

        public int SendAppendEntriesRPC_MaxRetryInfinityCounter { get; }

        public int SendRequestVoteRPC_MaxRetryInfinityCounter { get; }

        public int SendAppendEntriesRPC_MaxSessionCapacity { get; }

        public bool IncludeOriginalClientCommandInResults { get; }
        public bool IncludeOriginalConfigurationInResults { get; }
        public bool IncludeJointConsensusConfigurationInResults { get; }
        public bool IncludeConfigurationChangeRequestInResults { get; }

        #endregion

        #region Coracle Time Settings

        /// <summary>
        /// Max Election Timeout in Milliseconds. Used together with <see cref="MinElectionTimeout_InMilliseconds"/> to decide on a random timeout for Election Candidacy.
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
        /// Processing Timeout In Milliseconds when received an external Append Entries RPC
        /// </summary>
        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; }

        /// <summary>
        /// Determines how aggressively should the system check when the newly nodes added are caught up with the leader or not
        /// </summary>
        public int CatchupIntervalOnConfigurationChange_InMilliseconds { get; }

        /// <summary>
        /// Processing Timeout In Milliseconds when received an external Request Vote RPC
        /// </summary>
        public int RequestVoteTimeoutOnReceive_InMilliseconds { get; }

        /// <summary>
        /// Response Timeout In Milliseconds for an outgoing Request Vote RPC
        /// </summary>
        public int RequestVoteTimeoutOnSend_InMilliseconds { get; }

        /// <summary>
        /// Response Timeout In Milliseconds for an outgoing Append Entries RPC
        /// </summary>
        public int AppendEntriesTimeoutOnSend_InMilliseconds { get; }
        public Uri ThisNodeUri { get; }

        /// <summary>
        /// This determines how long to wait for an entry to be committed, i.e successful replication across a majority of clusters, 
        /// so that the leader can commit the log entry.
        /// 
        /// It can be equal to the ElectionTimeout only, since we can wait until that long.
        /// </summary>
        public int EntryCommitWaitTimeout_InMilliseconds { get; }

        /// <summary>
        /// This determines how aggressively to check for an entry to be committed, i.e successful replication across a majority of clusters, 
        /// so that the leader can commit the log entry.
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
        int CheckDepositionWaitInterval_InMilliseconds { get; }

        #endregion
    }
}


 
