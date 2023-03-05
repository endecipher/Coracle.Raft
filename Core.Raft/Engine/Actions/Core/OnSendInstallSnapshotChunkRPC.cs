using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Raft.Engine.Actions.Core
{
    /// <summary>
    /// The leader can decide where to place new entries in the log without consulting other servers, and data flows in a simple fashion
    /// from the leader to other servers
    /// <see cref="Section 5"/>
    /// 
    /// AppendEntriesRPC with LogEntries.Count > 0 to be sent to one single external node; only invoked from the leader.
    /// </summary>
    internal sealed class OnSendInstallSnapshotChunkRPC : BaseAction<OnSendInstallSnapshotChunkRPCContext, IInstallSnapshotRPCResponse>
    {
        #region Constants

        public const string ActionName = nameof(OnSendInstallSnapshotChunkRPC);
        public const string Sending = nameof(Sending);
        public const string RevertingToFollower = nameof(RevertingToFollower);
        public const string InvalidOffset = nameof(InvalidOffset);
        public const string callObject = nameof(callObject);
        public const string exception = nameof(exception);
        public const string towardsNode = nameof(towardsNode);
        public const string Received = nameof(Received);
        public const string responseObject = nameof(responseObject);
        public const string offset = nameof(offset);
        public const string SendingOnException = nameof(SendingOnException);
        public const string Successful = nameof(Successful);
        public const string ResendFull = nameof(ResendFull);
        #endregion

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Input.EngineConfiguration.InstallSnapshotChunkTimeoutOnSend_InMilliseconds);

        public override string UniqueName => ActionName;

        public OnSendInstallSnapshotChunkRPC(INodeConfiguration input, IStateDevelopment state, ISnapshotHeader snapshotHeader, int chunkOffset, OnSendInstallSnapshotChunkRPCContextDependencies actionDependencies, IActivityLogger activityLogger = null)
            : base(new OnSendInstallSnapshotChunkRPCContext(state, actionDependencies), activityLogger)
        {
            Input.NodeConfiguration = input;
            Input.SnapshotHeader = snapshotHeader;
            Input.Offset = chunkOffset;
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid
                && (Input.State as Leader).AppendEntriesManager.CanSendTowards(Input.NodeConfiguration.UniqueNodeId));
        }

        protected override async Task<IInstallSnapshotRPCResponse> Action(CancellationToken cancellationToken)
        {
            var isSnapshotStillAvailable = await Input.PersistentState.IsCommittedSnapshot(Input.SnapshotHeader);

            if (!isSnapshotStillAvailable)
            {
                await (Input.State as Leader).AppendEntriesManager.CancelInstallation(Input.NodeConfiguration.UniqueNodeId, Input.SnapshotHeader);

                return null;
            }

            var currentTerm = await Input.PersistentState.GetCurrentTerm();

            var (chunk, isLastOffset) = await Input.PersistentState.TryGetSnapshotChunk(Input.SnapshotHeader, Input.Offset);

            if (chunk != null)
            {
                var rpc = new InstallSnapshotRPC
                {
                    SnapshotId = Input.SnapshotHeader.SnapshotId,
                    Term = currentTerm,
                    LeaderId = Input.EngineConfiguration.NodeId,
                    LastIncludedIndex = Input.SnapshotHeader.LastIncludedIndex,
                    LastIncludedTerm = Input.SnapshotHeader.LastIncludedTerm,
                    Data = chunk,
                    Offset = Input.Offset,
                    Done = isLastOffset,
                };

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = Sending,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(callObject, rpc))
                .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                .WithCallerInfo());

                var result = await Input.RemoteManager.Send
                (
                    callObject: rpc,
                    configuration: Input.NodeConfiguration,
                    cancellationToken: cancellationToken
                );

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = Received,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(callObject, rpc))
                .With(ActivityParam.New(responseObject, result))
                .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                .WithCallerInfo());

                Leader leader = Input.State as Leader;

                if (!result.IsSuccessful) 
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = ActionName,
                        Event = SendingOnException,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(callObject, rpc))
                    .With(ActivityParam.New(responseObject, result))
                    .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                    .With(ActivityParam.New(exception, result.Exception))
                    .WithCallerInfo());

                    /// <remarks>
                    /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will fail.
                    /// Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
                    /// successfully. If a server crashes after completing an RPC but before responding, then it will receive the same RPC
                    /// again after it restarts. 
                    /// 
                    /// Raft RPCs are idempotent, so this causes no harm.
                    /// For example, if a follower receives an AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request.
                    /// <seealso cref="Section 5.5 Follower and Candidate Crashes"/>
                    /// </remarks
                    leader.AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId, Input.Offset);

                    return result.Response;
                }

                /// <remarks>
                /// All Servers: • If RPC request or response contains term T > currentTerm: set currentTerm = T, convert to follower (§5.1)
                /// <seealso cref="Figure 2 Rules For Servers"/>
                /// </remarks>

                if (result.Response.Term > rpc.Term)
                {
                    /// <remarks>
                    /// Current terms are exchanged whenever servers communicate; if one server’s current term is smaller than the other’s, 
                    /// then it updates its current term to the larger value.
                    /// <seealso cref="Section 5.1 Second-to-last para"/>
                    /// </remarks>

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = ActionName,
                        Event = RevertingToFollower,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(callObject, rpc))
                    .With(ActivityParam.New(responseObject, result))
                    .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                    .WithCallerInfo());

                    await Input.PersistentState.SetCurrentTerm(result.Response.Term);

                    Input.State.StateChanger.AbandonStateAndConvertTo<Follower>(nameof(Follower));
                }
                else if (result.Response.Resend)
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = ActionName,
                        Event = ResendFull,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(callObject, rpc))
                    .With(ActivityParam.New(responseObject, result))
                    .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                    .WithCallerInfo());

                    leader.AppendEntriesManager.UpdateFor(Input.NodeConfiguration.UniqueNodeId);

                    await leader.AppendEntriesManager.SendNextSnapshotChunk(Input.NodeConfiguration.UniqueNodeId, successfulOffset: Input.Offset, resendAll: true);
                }
                else
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = ActionName,
                        Event = Successful,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(callObject, rpc))
                    .With(ActivityParam.New(responseObject, result))
                    .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
                    .WithCallerInfo());

                    leader.AppendEntriesManager.UpdateFor(Input.NodeConfiguration.UniqueNodeId);

                    if (!isLastOffset)
                    {
                        await leader.AppendEntriesManager.SendNextSnapshotChunk(Input.NodeConfiguration.UniqueNodeId, successfulOffset: Input.Offset);
                    }
                    else
                    {
                        leader.LeaderProperties.UpdateIndices(Input.NodeConfiguration.UniqueNodeId, Input.SnapshotHeader.LastIncludedIndex);
                        await leader.AppendEntriesManager.CompleteInstallation(Input.NodeConfiguration.UniqueNodeId);
                    }
                }

                return result.Response;
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ActionName,
                Event = InvalidOffset,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(towardsNode, Input.NodeConfiguration))
            .With(ActivityParam.New(offset, Input.Offset))
            .WithCallerInfo());

            return null;
        }

        /// <remarks>
        /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will
        /// fail.Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
        /// successfully.If a server crashes after completing an RPC but before responding, then it will receive the same RPC
        /// again after it restarts.Raft RPCs are idempotent, so this causes no harm.For example, if a follower receives an
        /// AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request
        /// <seealso cref="Section 5.5 Follower and candidate crashes"/>
        /// </remarks>
        protected override Task OnTimeOut()
        {
            (Input.State as Leader).AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId, Input.Offset);

            return Task.CompletedTask;
        }

        protected override Task OnFailure()
        {
            (Input.State as Leader).AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId, Input.Offset);

            return Task.CompletedTask;
        }

        protected override Task OnCancellation()
        {
            (Input.State as Leader).AppendEntriesManager.IssueRetry(Input.NodeConfiguration.UniqueNodeId, Input.Offset);

            return Task.CompletedTask;
        }
    }
}
