using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States.LeaderState;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    internal interface ILeaderDependencies : IStateDependencies
    {
        IHeartbeatTimer HeartbeatTimer { get; set; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
        LeaderVolatileProperties LeaderProperties { get; set; }
        IAppendEntriesManager AppendEntriesManager { get; set; }
    }

    /// <summary>
    /// Raft implements consensus by first electing a distinguished leader, then giving the leader complete responsibility for managing the replicated log. 
    /// The leader accepts log entries from clients, replicates them on other servers, and tells servers when it is safe to apply log entries to their 
    /// state machines.
    /// 
    /// A leader can fail or become disconnected from the other servers, in which case a new leader is elected.
    /// <see cref="Section 5 Para 2"/>
    /// </summary>
    internal sealed class Leader : AbstractState, ILeaderDependencies
    {
        #region Constants
        private const string CheckForCommitIndexUpdateDueToSuccessfulResponse = nameof(CheckForCommitIndexUpdateDueToSuccessfulResponse);
        private const string NodeId = nameof(NodeId);
        private const string IsFromCommand = nameof(IsFromCommand);
        private const string SessionId = nameof(SessionId);
        private const string LeaderEntity = nameof(Leader);
        #endregion

        public Leader() : base() { StateValue = StateValues.Leader; }


        #region Additional Dependencies
        public IHeartbeatTimer HeartbeatTimer { get; set; }
        public ILeaderNodePronouncer LeaderNodePronouncer { get; set; }
        public LeaderVolatileProperties LeaderProperties { get; set; }
        public IAppendEntriesManager AppendEntriesManager { get; set; }
        #endregion

        public void SendHeartbeat(object state)
        {
            /// <summary>
            /// Servers retry RPCs if they do not receive a response in a timely manner, and they issue RPCs in parallel for best performance.
            /// <see cref="Section 5.1 End Para"/>
            /// 
            /// Leaders send periodic heartbeats(AppendEntries RPCs that carry no log entries) to all followers in order to maintain their authority.
            /// If a follower receives no communication over a period of time called the election timeout, then it assumes there is no viable leader 
            /// and begins an election to choose a new leader
            /// <see cref="Section 5.2 Leader Election"/>
            /// </summary>
            /// 
            AppendEntriesManager.InitiateAppendEntries();
        }

        protected override void OnElectionTimeout(object state) { }

        public override async Task OnStateChangeBeginDisposal()
        {
            await base.OnStateChangeBeginDisposal();

            LeaderProperties.Dispose();
            HeartbeatTimer.Dispose();
        }

        public override async Task InitializeOnStateChange(IVolatileProperties volatileProperties)
        {
            await base.InitializeOnStateChange(volatileProperties);

            StateValue = StateValues.Leader;

            //COMMENT: Recognize itself as Leader
            LeaderNodePronouncer.SetRunningNodeAsLeader();

            /// <remarks>
            /// Leader doesn't need to worry about Timeouts.
            /// However, a Heartbeat Timer is needed.
            /// </remarks>
            ElectionTimer.Dispose();

            /// <remarks>
            /// 
            /// Read-only operations can be handled without writing
            /// anything into the log. 
            /// 
            /// However, with no additional measures, this would run the risk of returning stale data, since
            /// the leader responding to the request might have been superseded by a newer leader of which it is unaware.
            /// 
            /// Linearizable reads must not return stale data, and Raft needs
            /// two extra precautions to guarantee this without using the
            /// log. 
            /// First, a leader must have the latest information on
            /// which entries are committed. The Leader Completeness
            /// Property guarantees that a leader has all committed entries, but at the start of its term, it may not know which
            /// those are.
            /// To find out, it needs to commit an entry from
            /// its term. Raft handles this by having each leader commit a blank no-op entry into the log at the start of its
            /// term.
            /// Second, a leader must check whether it has been deposed before processing a read - only request (its information may be stale if a more recent leader has been elected).
            /// Raft handles this by having the leader exchange heartbeat messages with a majority of the cluster before responding to read - only requests. Alternatively, the leader
            /// could rely on the heartbeat mechanism to provide a form
            /// of lease[9], but this would rely on timing for safety(it
            /// assumes bounded clock skew).
            /// 
            /// <see cref="Section 8 Client Interaction"/>
            /// 
            /// Append No-Op Entries with Current Term
            /// </remarks>
            var task = PersistentState.LogEntries.AppendNoOperationEntry();
            task.Wait();

            /// <remarks>
            /// Once a candidate wins an election, it becomes leader. 
            /// It then sends heartbeat messages to all of the other servers to establish its authority and prevent new elections.
            /// 
            /// <see cref="Section 5.2 Leader Election"/>
            /// </remarks>
            HeartbeatTimer.RegisterNew(SendHeartbeat);
        }


        /// <remarks>
        /// The leader decides when it is safe to apply a log entry to the state machines; such an entry is called committed. 
        /// Raft guarantees that committed entries are durable and will eventually be executed by all of the available state machines. 
        /// A log entry is committed once the leader that created the entry has replicated it on a majority of the servers (e.g., entry 7 in Figure 6). 
        /// This also commits all preceding entries in the leader’s log, including entries created by previous leaders
        /// <seealso cref="Section 5.3 Log Replication"/>
        /// </remarks>

        /// <summary>
        /// If there exists an N such that N > commitIndex, 
        /// a majority of matchIndex[i] ≥ N, and log[N].term == currentTerm: Set commitIndex = N
        /// 
        /// <see cref="Figure 2 Rules For Servers | Leaders"/>
        /// </summary>
        internal async Task CheckIfCommitIndexNeedsUpdatation(string externalServerId = null)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Success Response Received from {externalServerId} for AppendEntriesRPC",
                EntitySubject = LeaderEntity,
                Event = CheckForCommitIndexUpdateDueToSuccessfulResponse,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NodeId, externalServerId))
            .WithCallerInfo());

            var currentTerm = await PersistentState.GetCurrentTerm();

            var lastLogEntryIndexForTerm = await PersistentState.LogEntries.GetLastIndexForTerm(currentTerm);

            var firstLogEntryIndexForTerm = (await PersistentState.LogEntries.GetFirstIndexForTerm(currentTerm)).Value;

            for (long i = lastLogEntryIndexForTerm; i > VolatileState.CommitIndex && i > firstLogEntryIndexForTerm; i--)
            {
                /// <remarks>
                /// Raft never commits log entries from previous terms by counting replicas. Only log entries from the leader’s current
                /// term are committed by counting replicas; once an entry from the current term has been committed in this way, 
                /// then all prior entries are committed indirectly because of the Log Matching Property.
                /// 
                /// There are some situations where a leader could safely conclude that an older log entry is committed (for example, 
                /// if that entry is stored on every server), but Raft takes a more conservative approach for simplicity.
                /// <seealso cref="Section 5.4.2 Committing Entries from previous Terms"/>
                /// </remarks>
                long logTerm = await PersistentState.LogEntries.GetTermAtIndex(i);

                if (LeaderProperties.HasMatchIndexUpdatedForMajority(i) && logTerm == currentTerm)
                {
                    //TODO: Introduce Object Locks
                    UpdateCommitIndex(i);

                    return;
                }
            }
        }

        public override void HandleConfigurationChange(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {
            (AppendEntriesManager as IHandleConfigurationChange).HandleConfigurationChange(newPeerNodeConfigurations);
            (LeaderProperties as IHandleConfigurationChange).HandleConfigurationChange(newPeerNodeConfigurations);
        }
    }
}
