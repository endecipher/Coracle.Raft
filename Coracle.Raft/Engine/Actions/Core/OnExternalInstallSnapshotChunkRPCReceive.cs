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

using ActivityLogger.Logging;
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
using Coracle.Raft.Engine.Snapshots;
using Coracle.Raft.Engine.Configuration.Alterations;

namespace Coracle.Raft.Engine.Actions.Core
{
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

        #endregion

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.InstallSnapshotChunkTimeoutOnReceive_InMilliseconds);

        public OnExternalInstallSnapshotChunkRPCReceive(InstallSnapshotRPC input, IStateDevelopment state, OnExternalRPCReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnExternalRPCReceiveContext<InstallSnapshotRPC>(state, actionDependencies)
        {
            Request = input,
        }, activityLogger)
        { }

        public override string UniqueName => ActionName;

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
                fillAtOffset: Input.Request.Offset, receivedData: Input.Request.Data as ISnapshotDataChunk);

            /// Acknowledge Leader On Success
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

                    Input.ClusterConfigurationChanger.ChangeMembership(new MembershipUpdateEvent
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

        /// This is done so that NewResponsibilities can be configured AFTER we respond to this request.
        /// This is done to avoid the chances of current Task cancellation.
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
