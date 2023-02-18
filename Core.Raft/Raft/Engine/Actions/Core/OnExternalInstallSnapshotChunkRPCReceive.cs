﻿using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using Coracle.Raft.Engine.Configuration.Cluster;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Coracle.Raft.Engine.Actions.Core
{
    /// <remarks>
    ///  Raft maintains the following properties, which together constitute the Log Matching Property - 
    ///  • If two entries in different logs have the same index and term, then they store the same command.
    ///  • If two entries in different logs have the same index and term, then the logs are identical in all preceding entries.
    /// <seealso cref="Section 5.3 Log Replication"/>
    /// </remarks>
    /// 
    /// <summary>
    /// If we receive this, then surely there exists an External Leader Server. 
    /// </summary>
    internal sealed class OnExternalInstallSnapshotChunkRPCReceive : BaseAction<OnExternalRPCReceiveContext<InstallSnapshotRPC>, InstallSnapshotRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnExternalInstallSnapshotChunkRPCReceive);
        public const string DeniedDueToLesserTerm = nameof(DeniedDueToLesserTerm);
        public const string RevertingToFollower = nameof(RevertingToFollower);
        public const string Acknowledged = nameof(Acknowledged);
        public const string AcknowledgedAndWaiting = nameof(AcknowledgedAndWaiting);
        public const string ApplyingConfigurationFromSnapshot = nameof(ApplyingConfigurationFromSnapshot);
        public const string inputRequest = nameof(inputRequest);
        public const string parsedConfiguration = nameof(parsedConfiguration);
        public const string responding = nameof(responding);
        public const string DeniedDueToConflict = nameof(DeniedDueToConflict);

        #endregion

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.InstallSnapshotChunkTimeoutOnReceive_InMilliseconds);

        public OnExternalInstallSnapshotChunkRPCReceive(InstallSnapshotRPC input, IChangingState state, OnExternalRPCReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnExternalRPCReceiveContext<InstallSnapshotRPC>(state, actionDependencies)
        {
            Request = input,
        }, activityLogger)
        { }

        public override string UniqueName => ActionName;

        // If Should Proceed is false, due to any reason
        protected override InstallSnapshotRPCResponse DefaultOutput()
        {
            return new InstallSnapshotRPCResponse
            {
                Term = Input.PersistentState.GetCurrentTerm().GetAwaiter().GetResult(),
            };
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }

        /// <remarks>
        /// -Reply false if term < currentTerm (§5.1)
        /// -Reply false if log doesn’t contain an entry at prevLogIndex whose term matches prevLogTerm (§5.3)
        /// -If an existing entry conflicts with a new one(same index but different terms), delete the existing entry and all that
        /// follow it (§5.3)
        /// -Append any new entries not already in the log. 
        /// -If leaderCommit > commitIndex, set commitIndex = min(leaderCommit, index of last new entry)
        /// <seealso cref="Figure 2 Append Entries RPC"/>
        /// </remarks>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<InstallSnapshotRPCResponse> Action(CancellationToken cancellationToken)
        {
            /// <remarks>
            /// If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
            /// <seealso cref="Figure 2 Rules For Servers"/>
            /// 
            /// If a candidate or leader discovers that its term is out of date, it immediately reverts to follower state.
            /// <seealso cref="Section 5.1 Raft Basics"/>
            /// </remarks>

            /// <remarks>
            /// While waiting for votes, a candidate may receive an AppendEntries RPC from another server claiming to be leader. If the leader’s term 
            /// (included in its RPC) is at least as large as the candidate’s current term, then the candidate recognizes the leader as legitimate and 
            /// returns to follower state. If the term in the RPC is smaller than the candidate’s current term, then the candidate rejects the RPC and 
            /// continues in candidate state.
            /// 
            /// <seealso cref="Section 5.2 Leader Election"/>
            /// </remarks>


            var currentTerm = await Input.PersistentState.GetCurrentTerm();

            if (Input.State.StateValue.IsFollower())
            {
                /// <remarks>
                /// Leader Election: a new leader must be chosen when an existing leader fails
                /// <seealso cref="Section 5"/>
                /// 
                /// When we receive an External AppendEntriesRPC, we know that it can only be sent from a leader.
                /// Thus, we reset our ElectionTimer, so that Election is delayed. 
                /// </remarks>
                /// 
                (Input.State as Follower).AcknowledgeExternalRPC();
            }

            InstallSnapshotRPCResponse response;


            /// <remarks>
            /// When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes
            /// the new entries. If the follower does not find an entry in its log with the same index and term, then it refuses the new entries.
            /// 
            /// The consistency check acts as an induction step: the initial empty state of the logs satisfies the Log Matching Property, 
            /// and the consistency check preserves the Log Matching Property whenever logs are extended.
            /// 
            /// As a result, whenever AppendEntries returns successfully, the leader knows that the follower’s log is identical to its
            /// own log up through the new entries.
            /// <seealso cref="Section 5.3 Log Replication"/>
            /// </remarks>

            if (Input.Request.Term < currentTerm)
            {
                response = new InstallSnapshotRPCResponse
                {
                    Term = currentTerm,
                };

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = DeniedDueToLesserTerm,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .With(ActivityParam.New(responding, response))
                .WithCallerInfo());

                return response;
            }


            /// <remarks>
            /// All Servers: • If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
            /// <seealso cref="Figure 2 Rules For Servers"/>
            /// 
            /// Here, if we are in Candidate state, and we receive an ExternalAppendEntries, that means, 
            /// in the same Term, some rival server has become Leader, and in this case, we should turn Follower.
            /// </remarks>
            /// 
            if (Input.State.StateValue.IsCandidate() && Input.Request.Term >= currentTerm
                        || Input.State.StateValue.IsLeaderOrFollower() && Input.Request.Term > currentTerm)
            {
                currentTerm = Input.Request.Term;

                /// <remarks>
                /// Current terms are exchanged whenever servers communicate; if one server’s current term is smaller than the other’s, 
                /// then it updates its current term to the larger value.
                /// <seealso cref="Section 5.1 Second-to-last para"/>
                /// </remarks>
                /// 

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = RevertingToFollower,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .WithCallerInfo());

                await Input.PersistentState.SetCurrentTerm(currentTerm);

                Input.TurnToFollower = true;
            }


            var detail = await Input.PersistentState.FillSnapshotData(
                Input.Request.SnapshotId, 
                Input.Request.LastIncludedIndex, 
                Input.Request.LastIncludedTerm, 
                fillAtOffset: Input.Request.Offset, receivedData: Input.Request.Data as ISnapshotChunkData);

            // Acknowledge Leader On Success
            Input.LeaderNodePronouncer.SetNewLeader(Input.Request.LeaderId);

            response = new InstallSnapshotRPCResponse
            {
                Term = currentTerm,
            };

            if (Input.Request.Done)
            {
                bool isSnapshotValid = await Input.PersistentState.VerifySnapshot(detail, Input.Request.Offset);

                if (!isSnapshotValid)
                {
                    return new InstallSnapshotRPCResponse
                    {
                        Term = currentTerm,
                        Resend = true
                    };
                }

                var logEntry = await Input.PersistentState.TryGetValueAtIndex(Input.Request.LastIncludedIndex);

                if (logEntry != null && logEntry.Term.Equals(Input.Request.LastIncludedTerm))
                {
                    await Input.PersistentState.CommitAndUpdateLog(snapshotDetail: detail);
                }
                else
                {
                    await Input.PersistentState.CommitAndUpdateLog(snapshotDetail: detail, replaceAll: true);

                    IEnumerable<NodeConfiguration> clusterConfiguration = await Input.PersistentState.GetConfigurationFromSnapshot(detail);

                    Input.ClusterConfigurationChanger.ApplyConfiguration(new ClusterMembershipChange
                    {
                        Configuration = clusterConfiguration,
                    }, 
                    isInstallingSnapshot: true);

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = ActionName,
                        Event = ApplyingConfigurationFromSnapshot,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(inputRequest, Input.Request))
                    .With(ActivityParam.New(parsedConfiguration, clusterConfiguration))
                    .WithCallerInfo());

                    await (Input.State as AbstractState).ClientRequestHandler.ForceRebuildFromSnapshot(detail);

                    Input.State.VolatileState.OnSuccessfulSnapshotInstallation(detail);
                }

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = Acknowledged,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .With(ActivityParam.New(responding, response))
                .WithCallerInfo());
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = AcknowledgedAndWaiting,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(inputRequest, Input.Request))
                .With(ActivityParam.New(responding, response))
                .WithCallerInfo());
            }

            return response;
        }

        // This is done so that NewResponsibilities can be configured AFTER we respond to this request.
        // This is done to avoid the chances of current Task cancellation.
        protected override Task OnActionEnd()
        {
            if (Input.TurnToFollower)
            {
                Input.State.StateChanger.AbandonStateAndConvertTo<Follower>(nameof(Follower));
            }

            return base.OnActionEnd();
        }
    }
}
