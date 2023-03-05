using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.States;
using EntityMonitoring.FluentAssertions.Structure;
using FluentAssertions;
using Xunit;
using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Snapshots;
using Coracle.Raft.Engine.Command;
using Coracle.Raft.Examples.Data;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Tests.Framework;
using Coracle.Raft.Tests.Components.Helper;

namespace Coracle.Raft.Tests.Integration
{
    /// <summary>
    /// Tests the snapshot workflow, which comprises of:
    /// <list type="number">
    /// <item>Eligibility of Compaction</item>
    /// <item>Compaction Process</item>
    /// <item>InstallSnapshotRPC and successful transfer of the committed snapshot details to a newly joined node of the cluster</item>
    /// </list>
    /// </summary>
    [TestCaseOrderer($"Coracle.Raft.Tests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.Raft.Tests")]
    public class SnapshotWorkflowTest : BaseTest, IClassFixture<TestContext>
    {
        public SnapshotWorkflowTest(TestContext context) : base(context)
        {
        }

        [Fact]
        [Order(1)]
        public async Task IsTurningToLeader()
        {
            //Arrange
            Context.GetService<IActivityMonitor<Activity>>().Start();

            var notifiableQueue = CaptureActivities();

            Exception caughtException = null;
            CommandExecutionResult clientHandlingResult = null;
            var (Command, Note) = TestAddCommand();

            var majorityAttained = notifiableQueue
                .AttachNotifier(x => x.Is(ElectionManager.Entity, ElectionManager.MajorityAttained));

            var commitIndexUpdated = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(AbstractStateActivityConstants.Entity, AbstractStateActivityConstants.ApplyingLogEntry));

            StateCapture captureAfterCommand = null;

            //Act
            try
            {
                InitializeEngineConfiguration();
                CreateMockNode(MockNodeA);
                CreateMockNode(MockNodeB);
                RegisterMockNodeInRegistrar(MockNodeA);
                RegisterMockNodeInRegistrar(MockNodeB);

                InitializeNode();
                StartNode();

                EnqueueRequestVoteSuccessResponse(MockNodeA);
                EnqueueRequestVoteSuccessResponse(MockNodeB);

                var electionTimer = Context.GetService<IElectionTimer>() as TestElectionTimer;
                //This will make sure that the ElectionTimer callback invocation is approved for the Candidacy
                electionTimer.AwaitedLock.ApproveNext();

                ////Wait until Majority has been attained
                majorityAttained.Wait(EventNotificationTimeOut);

                var heartBeatTimer = Context.GetService<IHeartbeatTimer>() as TestHeartbeatTimer;

                EnqueueAppendEntriesSuccessResponse(MockNodeA);
                EnqueueAppendEntriesSuccessResponse(MockNodeB);

                //This will make sure that the Heartbeat callback invocation is approved for SendAppendEntries
                heartBeatTimer.AwaitedLock.ApproveNext();

                commitIndexUpdated.Wait(EventNotificationTimeOut);

                var isPronouncedLeaderSelf = Context.GetService<ILeaderNodePronouncer>().IsLeaderRecognized
                    && Context.GetService<ILeaderNodePronouncer>().RecognizedLeaderConfiguration.UniqueNodeId.Equals(SUT);

                await Task.Delay(50);

                EnqueueAppendEntriesSuccessResponse(MockNodeA);
                EnqueueAppendEntriesSuccessResponse(MockNodeB);

                clientHandlingResult = await Context.GetService<ICommandExecutor>()
                    .Execute(Command, CancellationToken.None);

                commitIndexUpdated.Wait(EventNotificationTimeOut);

                captureAfterCommand = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #region Assertions

            var assertableQueue = StartAssertions(notifiableQueue);

            caughtException
                .Should().Be(null, $" ");

            majorityAttained.IsConditionMatched.Should().BeTrue();
            commitIndexUpdated.IsConditionMatched.Should().BeTrue();

            Context.GetService<INoteStorage>()
                .TryGet(Note.UniqueHeader, out var note)
                .Should().BeTrue();

            note.Should().NotBeNull("Note must exist");

            note.Text.Should().Be(Note.Text, "Note should match the testNote supplied");

            captureAfterCommand
                .CommitIndex
                .Should().Be(2, "The command entry having index 2 should have been replicated to other nodes and also comitted");

            captureAfterCommand
                .LastApplied
                .Should().Be(2, "The command entry having index 2 should have been replicated to other nodes and also comitted");

            captureAfterCommand
                .MatchIndexes
                .Values
                .Should()
                .Match((i) => i.All(_ => _.Equals(2)),
                    $"{MockNodeA} and {MockNodeB} should have had replicated all entries up until the leader's last log index");

            captureAfterCommand
                .NextIndexes
                .Values
                .Should()
                .Match((i) => i.All(_ => _.Equals(3)),
                    $"{MockNodeA} and {MockNodeB} should have had replicated all entries up until the leader's last log index, and the nextIndex to send for each peer mock node should be one greater, i.e 3");

            Cleanup();
            #endregion
        }


        [Fact]
        [Order(2)]
        public async Task IsLeaderCommitingMultipleCommandEntriesAndInitiatingCompaction()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var commitIndexUpdated = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(AbstractStateActivityConstants.Entity, AbstractStateActivityConstants.ApplyingLogEntry));

            var snapshotBuilt = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(OnCompaction.ActionName, OnCompaction.LogCompacted)).RemoveOnceMatched();

            #endregion

            #region Act

            Exception caughtException = null;
            CommandExecutionResult clientHandlingResult = null;

            StateCapture captureAfterCommands = null;
            var threshold = Context.GetService<IEngineConfiguration>().SnapshotThresholdSize;

            var lastIndex = await Context.GetService<IPersistentStateHandler>().GetLastIndex();

            (bool HasSnapshot, ISnapshotHeader Detail) info = (false, null);

            var nextIndexToStart = lastIndex + 1;

            try
            {
                // Add new commands until threshold surpasses
                for (int i = (int)nextIndexToStart; i <= threshold + 1; i++)
                {
                    var (Command, Note) = TestAddCommand();

                    EnqueueAppendEntriesSuccessResponse(MockNodeA);
                    EnqueueAppendEntriesSuccessResponse(MockNodeB);

                    clientHandlingResult = await Context.GetService<ICommandExecutor>()
                        .Execute(Command, CancellationToken.None);

                    commitIndexUpdated.Wait(EventNotificationTimeOut);

                    if (!commitIndexUpdated.IsConditionMatched)
                        break;
                }

                captureAfterCommands = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());

                snapshotBuilt.Wait(EventNotificationTimeOut);

                info = await (Context.GetService<IPersistentStateHandler>() as SampleVolatileStateHandler).HasCommittedSnapshot(lastIndex);


            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assert

            var assertableQueue = StartAssertions(notifiableQueue);

            caughtException
                .Should().Be(null, $" ");

            captureAfterCommands
                .CommitIndex
                .Should().Be(threshold + 1, "The commit Index should be 6, as the comands have been replicated to other nodes");

            snapshotBuilt.IsConditionMatched.Should().BeTrue();

            info.HasSnapshot.Should().BeTrue();

            info.Detail.LastIncludedTerm.Should().Be(captureAfterCommands.CurrentTerm, "Term should be the same, as it has not changed");

            info.Detail.LastIncludedIndex.Should().Be(threshold, "The snapshot should encompass the appropriate index");

            (await Context.GetService<IPersistentStateHandler>().FetchLogEntryIndexPreviousToIndex(info.Detail.LastIncludedIndex))
                .Should()
                .Be(0, "As the Snapshot shohuld be the second log entry in the log chain, and sit next to the None entry from initialization");

            Cleanup();
            #endregion
        }


        [Fact]
        [Order(3)]
        public async Task IsLeaderHandlingConfigurationChangeAndSendingSnapshot()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var commitIndexUpdated = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(AbstractStateActivityConstants.Entity, AbstractStateActivityConstants.ApplyingLogEntry)).RemoveOnceMatched();

            var configurationChanged = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(OnConfigurationChangeRequestReceive.ActionName, OnConfigurationChangeRequestReceive.ConfigurationChangeSuccessful)).RemoveOnceMatched();

            var successfulInstallation = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(OnSendInstallSnapshotChunkRPC.ActionName, OnSendInstallSnapshotChunkRPC.Successful)).RemoveOnceMatched();

            var nodesCaughtUp = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(OnCatchUpOfNewlyAddedNodes.ActionName, OnCatchUpOfNewlyAddedNodes.NodesCaughtUp)).RemoveOnceMatched();

            var currentConfiguration = Context.GetService<IClusterConfiguration>().CurrentConfiguration;

            var newConfiguration = currentConfiguration.Select(x => new NodeConfiguration
            {
                UniqueNodeId = x.UniqueNodeId

            }).Append(new NodeConfiguration
            {
                UniqueNodeId = MockNewNodeC //Adding NewNodeC

            }).Where(x => !x.UniqueNodeId.Equals(MockNodeB)) //Removing MockNodeIdB
            .ToList();

            CreateMockNode(MockNewNodeC);

            var snapshotDetail = await Context.GetService<IPersistentStateHandler>().GetCommittedSnapshot();

            var file = await Context.GetService<ISnapshotManager>().GetFile(snapshotDetail);

            int lastOffset = await file.GetLastOffset();

            #endregion

            #region Act

            Exception caughtException = null;
            ConfigurationChangeResult changeResult = null;
            StateCapture captureAfterConfigurationChange = null;

            try
            {
                /// <remarks>
                /// Enqueuing Responses for old nodes
                /// </remarks>
                EnqueueAppendEntriesSuccessResponse(MockNodeA); //For Replication Approval of C-old,new
                EnqueueAppendEntriesSuccessResponse(MockNodeB); //For Replication Approval of C-old,new

                EnqueueAppendEntriesSuccessResponse(MockNodeA); //For Replication Approval of C-new
                EnqueueAppendEntriesSuccessResponse(MockNodeB); //For Replication Approval of C-new

                EnqueueAppendEntriesSuccessResponse(MockNodeA); //For any heartbeats being sent for the C-new entry to be replicated and then applying cluster configuration
                EnqueueAppendEntriesSuccessResponse(MockNodeB); //For any heartbeats being sent for the C-new entry to be replicated and then applying cluster configuration

                /// <remarks>
                /// Enqueueing responses for the new node
                /// </remarks>

                for (int i = 0; i <= lastOffset; i++)
                {
                    EnqueueInstallSnapshotSuccessResponse(MockNewNodeC); //Enqueue Success Responses for each SnapshotChunkRPC
                }

                EnqueueAppendEntriesSuccessResponse(MockNewNodeC); //Enqueue Success AppendEntries for the remaining entries
                EnqueueAppendEntriesSuccessResponse(MockNewNodeC); //For any heartbeats being sent for the C-new entry to be replicated and then applying cluster configuration

                changeResult = await Context.GetService<IConfigurationRequestExecutor>().IssueChange(new ConfigurationChangeRequest
                {
                    UniqueRequestId = Guid.NewGuid().ToString(),
                    NewConfiguration = newConfiguration

                }, CancellationToken.None);

                successfulInstallation.Wait(EventNotificationTimeOut);

                nodesCaughtUp.Wait(EventNotificationTimeOut);

                configurationChanged.Wait(EventNotificationTimeOut);

                captureAfterConfigurationChange = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assert

            var assertableQueue = StartAssertions(notifiableQueue);

            caughtException
                .Should().Be(null, $" ");

            Context.NodeContext.GetMockNode(MockNewNodeC).InstallSnapshotLock.RemoteCalls.Count
                .Should().Be(lastOffset + 1, "As, if the last offset is 1, then there should be 2 calls (one for 0 offset and one for 1 offset)");

            var lastIndex = await Context.GetService<IPersistentStateHandler>().GetLastIndex();

            captureAfterConfigurationChange
                .CommitIndex
                .Should().Be(lastIndex, "The conf entry should have been replicated to other nodes and also comitted");

            captureAfterConfigurationChange
                .LastApplied
                .Should().Be(lastIndex, "The conf entry should have been replicated to other nodes and also comitted");

            captureAfterConfigurationChange
                .MatchIndexes
                .Values
                .Should()
                .Match((i) => i.All(_ => _.Equals(lastIndex)),
                    $"{MockNodeA} and {MockNodeB} should have had replicated all entries up until the leader's last log index");

            captureAfterConfigurationChange
                .NextIndexes
                .Values
                .Should()
                .Match((i) => i.All(_ => _.Equals(lastIndex + 1)),
                    $"{MockNodeA} and {MockNodeB} should have had replicated all entries up until the leader's last log index, and the nextIndex to send for each peer mock node should be one greater");

            Cleanup();
            #endregion
        }
    }
}
