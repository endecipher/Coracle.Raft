using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using EntityMonitoring.FluentAssertions.Structure;
using FluentAssertions;
using Xunit;
using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Tests.Framework;
using Coracle.Raft.Examples.Data;

namespace Coracle.Raft.Tests.Integration
{
    /// <summary>
    /// Tests a follower node's abilities:
    /// <list type="number">
    /// <item>Updating Term to a higher value</item>
    /// <item>Replication and handing of AppendEntriesRPCs</item>
    /// <item>Usage of Leader's Commit Index</item>
    /// <item>Snapshot installation</item>
    /// <item>AppendEntriesRPC to sync entries post snapshot installation</item>
    /// </list>
    /// </summary>
    [TestCaseOrderer($"Coracle.Raft.Tests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.Raft.Tests")]
    public class FollowerWorkflowTest : BaseTest, IClassFixture<TestContext>
    {
        public FollowerWorkflowTest(TestContext context) : base(context)
        {
        }

        [Fact]
        [Order(1)]
        public async Task IsFollowerUpdatingTermToHigherReceivedValue()
        {
            #region Arrange

            Exception caughtException = null;
            InitializeEngineConfiguration();
            CreateMockNode(MockNodeA);
            CreateMockNode(MockNodeB);
            RegisterMockNodeInRegistrar(MockNodeA);
            RegisterMockNodeInRegistrar(MockNodeB);
            InitializeNode();
            StartNode();

            RemoteCallResult<IRequestVoteRPCResponse> requestVoteResponse = null;

            Context.GetService<IActivityMonitor<Activity>>().Start();

            var queue = CaptureActivities();

            var greaterTermEncountered = queue
                .AttachNotifier(_ => _.Is(OnExternalRequestVoteRPCReceive.ActionName, OnExternalRequestVoteRPCReceive.BeingFollowerAsGreaterTermReceived))
                .RemoveOnceMatched();

            #endregion

            #region Act

            var term = await Context.GetService<IPersistentStateHandler>().GetCurrentTerm();
            var entry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

            try
            {
                requestVoteResponse = await Context.GetService<IRemoteCallExecutor>().RespondTo(new RequestVoteRPC()
                {
                    CandidateId = MockNodeA,
                    LastLogIndex = entry.CurrentIndex,
                    LastLogTerm = entry.Term,
                    Term = term + 1
                }, CancellationToken.None);

                greaterTermEncountered.Wait(EventNotificationTimeOut);

            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterRequestVote = CaptureStateProperties();

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            greaterTermEncountered
                .IsConditionMatched
                .Should().BeTrue("Concerned activity should be logged");

            term
                .Should().Be(0, "As the node has just started and not yet timed out for election");

            entry
                .CurrentIndex
                .Should().Be(0, "As it's the initialized entry for the log holder");

            entry
                .Type
                .Should().Be(LogEntry.Types.None, "As this is the initialized entry type");

            afterRequestVote
                .CurrentTerm
                .Should().Be(1, "As the request vote was passed with a higher term, the follower should have updated its term internally to the higher value");

            afterRequestVote
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterRequestVote
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should be granted to the first sender of the RPC");

            requestVoteResponse
                .IsSuccessful
                .Should().BeTrue("As a RPC response is expected without a failure");

            requestVoteResponse
                .Response.Term
                .Should().Be(1, "As it should reply with the updated higher term when it encoutered it");

            requestVoteResponse
                .Response.VoteGranted
                .Should().BeTrue("As Vote should be granted to the first sender");

            #endregion
        }


        [Fact]
        [Order(2)]
        public async Task IsFollowerWritingEntriesSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            RemoteCallResult<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var writingEntries = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.OverwritingEntriesIfAny))
                .RemoveOnceMatched();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentStateHandler>().GetCurrentTerm();

            LogEntry senderLeaderlogEntry = new LogEntry
            {
                CurrentIndex = 1,
                Type = LogEntry.Types.NoOperation,
                Term = 1,
                Content = null
            };

            try
            {
                appendEntriesResponse = await Context.GetService<IRemoteCallExecutor>().RespondTo(new AppendEntriesRPC(entries: new List<LogEntry>
                {
                    senderLeaderlogEntry
                })
                {
                    LeaderCommitIndex = 0, //As the sender (who is now the leader) has not yet sent an outgoing AppendEntries RPC
                    PreviousLogIndex = 0,
                    PreviousLogTerm = 0,
                    LeaderId = MockNodeA,
                    Term = 1
                }, CancellationToken.None);

                writingEntries.Wait(EventNotificationTimeOut);

            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterAppendEntries = CaptureStateProperties();

            var lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            writingEntries
                .IsConditionMatched
                .Should().BeTrue("Concerned activity should be logged");

            lastEntry
                .CurrentIndex
                .Should().Be(senderLeaderlogEntry.CurrentIndex, "As the entry should have been written successfully");

            lastEntry
                .Term
                .Should().Be(senderLeaderlogEntry.Term, "As the entry should have been written successfully");

            lastEntry
                .Type
                .Should().Be(senderLeaderlogEntry.Type, "As the entry should have been written successfully");

            afterAppendEntries
                .CurrentTerm
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            afterAppendEntries
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterAppendEntries
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should still be granted to the previous sender");

            afterAppendEntries
                .CommitIndex
                .Should().Be(0, "As the leader commit index sent was 0, the follower should still be having leader commit 0. Only if the leader commit > local commit, can something happen");

            appendEntriesResponse
                .IsSuccessful
                .Should().BeTrue("As a RPC response is expected without a failure");

            appendEntriesResponse
                .Response.Term
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            appendEntriesResponse
                .Response.Success
                .Should().BeTrue("As entries are non-conflicting and it should be successful");

            appendEntriesResponse
               .Response.ConflictingEntryTermOnFailure
               .Should().BeNull("As entries are non-conflicting and it should be successful");

            appendEntriesResponse
               .Response.FirstIndexOfConflictingEntryTermOnFailure
               .Should().BeNull("As entries are non-conflicting and it should be successful");
            #endregion
        }


        [Fact]
        [Order(3)]
        public async Task IsFollowerRespondingToHeartbeatSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            RemoteCallResult<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var acknowledgedHeartbeat = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.Acknowledged))
                .RemoveOnceMatched();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentStateHandler>().GetCurrentTerm();

            try
            {
                appendEntriesResponse = await Context.GetService<IRemoteCallExecutor>().RespondTo(new AppendEntriesRPC(entries: null)
                {
                    LeaderCommitIndex = 1, //As the sender (who is the leader) has received the response from the SUT, and has now replicated the NoOp entry, and thus has commitIndex as 1
                    PreviousLogIndex = 1, //Since the entries sent are null, the prev entry is nothing but the last entry which is the NoOp entry which has index 1, after the None entry from initialization
                    PreviousLogTerm = 1, //Since the entries sent are null, the prev entry is nothing but the last entry which is the NoOp entry which has term 1, after the None entry from initialization
                    LeaderId = MockNodeA,
                    Term = 1 //Term is still 1, as the leader is in control 
                }, CancellationToken.None);

                acknowledgedHeartbeat.Wait(EventNotificationTimeOut);

            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterAppendEntries = CaptureStateProperties();

            var lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            acknowledgedHeartbeat
                .IsConditionMatched
                .Should().BeTrue("Concerned activity should be logged");

            lastEntry
                .CurrentIndex
                .Should().Be(1, "As no additional entry should have been appended");

            lastEntry
                .Term
                .Should().Be(1, "As no additional entry should have been appended");

            lastEntry
                .Type
                .Should().Be(LogEntry.Types.NoOperation, "As no additional entry should have been appended");

            afterAppendEntries
                .CurrentTerm
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            afterAppendEntries
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterAppendEntries
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should still be granted to the previous sender");

            afterAppendEntries
                .CommitIndex
                .Should().Be(0, "As the leader commit index sent is now 1, the follower can update its own commit index but ONLY during new entries being sent. Therefore, it will still remain 0. Checked using RAFT scope too");

            appendEntriesResponse
                .IsSuccessful
                .Should().BeTrue("As a RPC response is expected without a failure");

            appendEntriesResponse
                .Response.Term
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            appendEntriesResponse
                .Response.Success
                .Should().BeTrue("As acknowledgement should be successful");

            appendEntriesResponse
               .Response.ConflictingEntryTermOnFailure
               .Should().BeNull("As acknowledgement should be successful");

            appendEntriesResponse
               .Response.FirstIndexOfConflictingEntryTermOnFailure
               .Should().BeNull("As acknowledgement should be successful");
            #endregion
        }


        [Fact]
        [Order(4)]
        public async Task AreCommandEntriesBeingReplicatedTofollowerSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            RemoteCallResult<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var writingEntries = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.OverwritingEntriesIfAny))
                .RemoveOnceMatched();

            var (command, note) = TestAddCommand();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentStateHandler>().GetCurrentTerm();

            var commandLogEntry = new LogEntry
            {
                CurrentIndex = 2,
                Term = 1,
                Type = LogEntry.Types.Command,
                Content = command
            };

            try
            {
                appendEntriesResponse = await Context.GetService<IRemoteCallExecutor>().RespondTo(new AppendEntriesRPC(entries: new List<LogEntry>
                {
                    commandLogEntry
                })
                {
                    LeaderCommitIndex = 1, //As the sender (who is the leader) has received the response from the SUT, and has now replicated the NoOp entry, and thus has commitIndex as 1
                    PreviousLogIndex = 1, //The prev entry is nothing but the last entry which is the NoOp entry which has index 1, after the None entry from initialization
                    PreviousLogTerm = 1, //The prev entry is nothing but the last entry which is the NoOp entry which has index 1, after the None entry from initialization
                    LeaderId = MockNodeA,
                    Term = 1 //Term is still 1, as the leader is in control 
                }, CancellationToken.None);

                writingEntries.Wait(EventNotificationTimeOut);
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterAppendEntries = CaptureStateProperties();

            var lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            writingEntries
                .IsConditionMatched
                .Should().BeTrue("Concerned activity should be logged");

            lastEntry
                .CurrentIndex
                .Should().Be(2, "As an additional command entry should have been appended");

            lastEntry
                .Term
                .Should().Be(1, "As the term should be same as before");

            lastEntry
                .Type
                .Should().Be(LogEntry.Types.Command, "As the appended entry must be a command entry");

            afterAppendEntries
                .CurrentTerm
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            afterAppendEntries
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterAppendEntries
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should still be granted to the previous sender");

            afterAppendEntries
                .LastApplied
                .Should().Be(1, "As the leader commit index sent is now 1, the follower can update its own commit index as new entries ARE ALSO being sent. Therefore, it should update to 1, and the NoOp entry must be applied");

            afterAppendEntries
                .CommitIndex
                .Should().Be(1, "As the leader commit index sent is now 1, the follower can update its own commit index as new entries ARE ALSO being sent. Therefore, it should update to 1, and the NoOp entry must be applied");

            appendEntriesResponse
                .IsSuccessful
                .Should().BeTrue("As a RPC response is expected without a failure");

            appendEntriesResponse
                .Response.Term
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            appendEntriesResponse
                .Response.Success
                .Should().BeTrue("As acknowledgement should be successful");

            appendEntriesResponse
               .Response.ConflictingEntryTermOnFailure
               .Should().BeNull("As there should be no conflicts");

            appendEntriesResponse
               .Response.FirstIndexOfConflictingEntryTermOnFailure
               .Should().BeNull("As there should be no conflicts");

            #endregion
        }


        [Fact]
        [Order(5)]
        public async Task IsFollowerSkippingApplyingCommandsDuringHeartbeatSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            RemoteCallResult<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var acknowledgedHeartbeat = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.Acknowledged))
                .RemoveOnceMatched();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentStateHandler>().GetCurrentTerm();

            try
            {
                appendEntriesResponse = await Context.GetService<IRemoteCallExecutor>().RespondTo(new AppendEntriesRPC(entries: null)
                {
                    LeaderCommitIndex = 2, //As the sender (who is the leader) has received the response from the SUT, and has now replicated not only the NoOp entry, but the command entry as well; thus has commitIndex as 2
                    PreviousLogIndex = 2, //Since the entries sent are null, the prev entry is nothing but the last entry which is the command entry which has index 2, after the NoOp entry 
                    PreviousLogTerm = 1, //Since the entries sent are null, the prev entry is nothing but the last entry which is the command entry which has index 2, after the NoOp entry 
                    LeaderId = MockNodeA,
                    Term = 1 //Term is still 1, as the leader is in control 
                }, CancellationToken.None);

                acknowledgedHeartbeat.Wait(EventNotificationTimeOut);

            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterAppendEntries = CaptureStateProperties();

            var lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            acknowledgedHeartbeat
                .IsConditionMatched
                .Should().BeTrue("Concerned activity should be logged");

            lastEntry
                .CurrentIndex
                .Should().Be(2, "As no additional entry should have been appended");

            lastEntry
                .Term
                .Should().Be(1, "As no additional entry should have been appended");

            lastEntry
                .Type
                .Should().Be(LogEntry.Types.Command, "As no additional entry should have been appended");

            afterAppendEntries
                .CurrentTerm
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            afterAppendEntries
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterAppendEntries
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should still be granted to the previous sender");

            afterAppendEntries
                .CommitIndex // Important
                .Should().Be(1, "As the leader commit index sent is now 2, the follower can update its own commit index but ONLY during new entries being sent. Therefore, it will still remain 1. Checked using RAFT scope too");

            appendEntriesResponse
                .IsSuccessful
                .Should().BeTrue("As a RPC response is expected without a failure");

            appendEntriesResponse
                .Response.Term
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            appendEntriesResponse
                .Response.Success
                .Should().BeTrue("As acknowledgement should be successful");

            appendEntriesResponse
               .Response.ConflictingEntryTermOnFailure
               .Should().BeNull("As acknowledgement should be successful");

            appendEntriesResponse
               .Response.FirstIndexOfConflictingEntryTermOnFailure
               .Should().BeNull("As acknowledgement should be successful");
            #endregion
        }


        [Fact]
        [Order(6)]
        public async Task IsFollowerApplyingCommandEntriesSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            RemoteCallResult<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var writingEntries = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.OverwritingEntriesIfAny))
                .RemoveOnceMatched();

            var commitIndexUpdated = queue
                .AttachNotifier(_ => _.Is(AbstractStateActivityConstants.Entity, AbstractStateActivityConstants.ApplyingLogEntry))
                .RemoveOnceMatched();

            var lastCommandCounter = Context.CommandContext.GetLatestCommandCounter;

            var (newCommand, newNote) = TestAddCommand();

            var previouslyAddedCommandEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtIndex(2);

            var previouslyAddedCommand = await Context.GetService<IPersistentStateHandler>().ReadFrom<NoteCommand>(previouslyAddedCommandEntry);

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentStateHandler>().GetCurrentTerm();

            var commandLogEntry = new LogEntry
            {
                CurrentIndex = 3,
                Term = 1,
                Type = LogEntry.Types.Command,
                Content = newCommand
            };

            try
            {
                appendEntriesResponse = await Context.GetService<IRemoteCallExecutor>().RespondTo(new AppendEntriesRPC(entries: new List<LogEntry>
                {
                    commandLogEntry
                })
                {
                    LeaderCommitIndex = 2, //As the sender (who is the leader) has received the response from the SUT, and has now replicated not only the NoOp entry, but the command entry as well; thus has commitIndex as 2
                    PreviousLogIndex = 2, //The prev entry is nothing but the last entry which is the command entry which has index 2, after the NoOp entry 
                    PreviousLogTerm = 1, //The prev entry is nothing but the last entry which is the command entry which has term 1, after the NoOp entry 
                    LeaderId = MockNodeA,
                    Term = 1 //Term is still 1, as the leader is in control 
                }, CancellationToken.None);

                writingEntries.Wait(EventNotificationTimeOut);

                commitIndexUpdated.Wait(EventNotificationTimeOut);
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterAppendEntries = CaptureStateProperties();

            var lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

            bool isPreviousNoteCommandExecuted = Context.GetService<INoteStorage>().TryGet(GetNoteHeader(lastCommandCounter), out var previousNote);

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            writingEntries
                .IsConditionMatched
                .Should().BeTrue("Concerned activity should be logged");

            lastEntry
                .CurrentIndex
                .Should().Be(3, "As an additional command entry should have been appended");

            lastEntry
                .Term
                .Should().Be(1, "As the term should be same as before");

            lastEntry
                .Type
                .Should().Be(LogEntry.Types.Command, "As the appended entry must be a command entry");

            isPreviousNoteCommandExecuted
                .Should().BeTrue("As the previous command entry should be successfully applied and executed to the state machine");

            previousNote
                .Should().NotBeNull();

            Serialized(previouslyAddedCommand.Data)
                .Should().Contain(previousNote.Text, "The Log Entry serialized data should contain the previous note text, meaning that it was executed successfully");

            afterAppendEntries
                .CurrentTerm
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            afterAppendEntries
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterAppendEntries
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should still be granted to the previous sender");

            afterAppendEntries
                .LastApplied
                .Should().Be(2, "As the leader commit index sent is now 2, the follower can update its own commit index as new entries ARE ALSO being sent. Therefore, it should update to 2, and the command entry must be applied AND executed");

            afterAppendEntries
                .CommitIndex
                .Should().Be(2, "As the leader commit index sent is now 2, the follower can update its own commit index as new entries ARE ALSO being sent. Therefore, it should update to 2, and the command entry must be applied AND executed");

            appendEntriesResponse
                .IsSuccessful
                .Should().BeTrue("As a RPC response is expected without a failure");

            appendEntriesResponse
                .Response.Term
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            appendEntriesResponse
                .Response.Success
                .Should().BeTrue("As acknowledgement should be successful");

            appendEntriesResponse
               .Response.ConflictingEntryTermOnFailure
               .Should().BeNull("As there should be no conflicts");

            appendEntriesResponse
               .Response.FirstIndexOfConflictingEntryTermOnFailure
               .Should().BeNull("As there should be no conflicts");



            #endregion
        }


        [Fact]
        [Order(7)]
        public async Task IsFollowerRebuildingStateOnSnapshotSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            var (newCommand1, newNote1) = TestAddCommand();
            var (newCommand2, newNote2) = TestAddCommand();

            var followerConfigurationBeforeSnapshot = Context.GetService<IClusterConfiguration>().CurrentConfiguration;

            var followerLogEntriesBeforeSnapshot = await GetAllLogEntries();

            var followerLastEntryBeforeSnapshot = followerLogEntriesBeforeSnapshot.Last();

            var logEntriesToCompact = new List<LogEntry>(followerLogEntriesBeforeSnapshot);

            logEntriesToCompact.AddRange(new[]
            {
                new LogEntry
                {
                    CurrentIndex = followerLastEntryBeforeSnapshot.CurrentIndex + 1,
                    Type = LogEntry.Types.Command,
                    Term = 1,
                    Content = newCommand1
                },
                new LogEntry
                {
                    CurrentIndex = followerLastEntryBeforeSnapshot.CurrentIndex + 2,
                    Type = LogEntry.Types.Command,
                    Term = 2,
                    Content = newCommand2
                },
            });

            var leaderSnapshotManager = new SnapshotManager(Context.GetService<IActivityLogger>());
            var snapshot = await leaderSnapshotManager.CreateFile(logEntriesToCompact, followerConfigurationBeforeSnapshot);
            var snapshotFile = await leaderSnapshotManager.GetFile(snapshot);
            var snapshotFileLastOffset = await snapshotFile.GetLastOffset();

            Queue<RemoteCallResult<IInstallSnapshotRPCResponse>> installationResponses = new Queue<RemoteCallResult<IInstallSnapshotRPCResponse>>();

            var queue = CaptureActivities();

            var rebuildingNotes = queue
                .AttachNotifier(_ => _.Is(NoteStorage.NoteEntity, NoteStorage.Rebuilt))
                .RemoveOnceMatched();

            var applyingConfigurationFromSnapshot = queue
                .AttachNotifier(_ => _.Is(OnExternalInstallSnapshotChunkRPCReceive.ActionName, OnExternalInstallSnapshotChunkRPCReceive.ApplyingConfigurationFromSnapshot))
                .RemoveOnceMatched();

            var acknowledgedSnapshots = queue
                .AttachNotifier(_ => _.Is(OnExternalInstallSnapshotChunkRPCReceive.ActionName, OnExternalInstallSnapshotChunkRPCReceive.Acknowledged))
                .RemoveOnceMatched();

            #endregion

            #region Act

            try
            {
                for (int offset = 0; offset <= snapshotFileLastOffset; offset++)
                {
                    var chunkData = await snapshotFile.ReadDataAt(offset);

                    var response = await Context.GetService<IRemoteCallExecutor>().RespondTo(new InstallSnapshotRPC
                    {
                        Term = 2,
                        LastIncludedTerm = snapshot.LastIncludedTerm,
                        LastIncludedIndex = snapshot.LastIncludedIndex,
                        SnapshotId = snapshot.SnapshotId,
                        LeaderId = MockNodeA,
                        Data = chunkData,
                        Offset = offset,
                        Done = offset == snapshotFileLastOffset
                    }, CancellationToken.None);

                    installationResponses.Enqueue(response);
                }

                applyingConfigurationFromSnapshot.Wait(EventNotificationTimeOut);

                rebuildingNotes.Wait(EventNotificationTimeOut);

                acknowledgedSnapshots.Wait(EventNotificationTimeOut);
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterSnapshot = CaptureStateProperties();

            var followerEntriesAfterSnapshot = await GetAllLogEntries();

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            applyingConfigurationFromSnapshot.IsConditionMatched.Should().BeTrue();
            rebuildingNotes.IsConditionMatched.Should().BeTrue();
            acknowledgedSnapshots.IsConditionMatched.Should().BeTrue();

            followerEntriesAfterSnapshot.Count
                .Should()
                .BeLessThan(followerLogEntriesBeforeSnapshot.Count, "As the logEntries were replaced with a more compact snapshot");

            followerLastEntryBeforeSnapshot
                .CurrentIndex
                .Should().BeLessThan(followerEntriesAfterSnapshot.Last().CurrentIndex, "As additional entries were sent as part of the snapshot");

            afterSnapshot
                .CurrentTerm
                .Should().Be(2, $"As Follower should update its term, since an external RPC is received with a higher term");

            afterSnapshot
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterSnapshot
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should still be granted to the previous sender");

            afterSnapshot
                .LastApplied
                .Should().Be(snapshot.LastIncludedIndex, "As the follower has applied the snapshot state");

            afterSnapshot
                .CommitIndex
                .Should().Be(snapshot.LastIncludedIndex, "As the follower has applied the snapshot state, it makes sense that it has also updated commit index");

            installationResponses.Select(_ => _.Response).First()
                .Term
                .Should().Be(2, "As the term updation should also be reflected when sending the response back");

            Context.GetService<INoteStorage>().HasNote(newNote2).Should().BeTrue("As the notes was rebuilt from the snapshot, it should be present");

            Cleanup();
            #endregion
        }


        [Fact]
        [Order(8)]
        public async Task IsFollowerApplyingCommandEntriesSuccessfullyAfterSnapshot()
        {
            #region Arrange

            Exception caughtException = null;

            RemoteCallResult<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var writingEntries = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.OverwritingEntriesIfAny))
                .RemoveOnceMatched();

            var commitIndexUpdated = queue
                .AttachNotifier(_ => _.Is(AbstractStateActivityConstants.Entity, AbstractStateActivityConstants.ApplyingLogEntry))
                .RemoveOnceMatched();

            var followerLogEntriesBeforeRPC = await GetAllLogEntries();

            var (newCommand, newNote) = TestAddCommand();
            var (newCommand2, newNote2) = TestAddCommand();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentStateHandler>().GetCurrentTerm();

            var commandEntry1_LeaderCommitted = new LogEntry
            {
                CurrentIndex = followerLogEntriesBeforeRPC.Last().CurrentIndex + 1,
                Term = termBeforeRPC,
                Type = LogEntry.Types.Command,
                Content = newCommand
            };

            var commandEntry2_LeaderUnCommitted = new LogEntry
            {
                CurrentIndex = followerLogEntriesBeforeRPC.Last().CurrentIndex + 2,
                Term = termBeforeRPC,
                Type = LogEntry.Types.Command,
                Content = newCommand2
            };

            try
            {
                /// <remarks>
                /// Considering a scenario that the Leader has made sure this follower node as the snapshot, but since snapshot are always made in a stable context,
                /// We can be sure that there are more entries that belong to the leader. 
                /// 
                /// Thus, Snapshot.LastIncludedIndex < Leader Commit Index
                /// 
                /// Another thing is, the leader will then send the entries between the MatchIndex (i.e Snapshot.LastIncludedIndex) and NextIndex (i.e LastLogIndex), it may be the case that the LastLogIndex is more than the CommitIndex, and this node is trying to replicate that as well.
                /// Therefore, we can have 2 command entries. 1 which would be committed, and one which would be not.
                /// 
                /// And thus, command1 would have been applied, but not command2. 
                /// </remarks>

                appendEntriesResponse = await Context.GetService<IRemoteCallExecutor>().RespondTo(new AppendEntriesRPC(entries: new List<LogEntry>
                {
                    commandEntry1_LeaderCommitted, commandEntry2_LeaderUnCommitted
                })
                {
                    LeaderCommitIndex = commandEntry1_LeaderCommitted.CurrentIndex,
                    PreviousLogIndex = commandEntry1_LeaderCommitted.CurrentIndex - 1,
                    PreviousLogTerm = commandEntry1_LeaderCommitted.Term,
                    LeaderId = MockNodeA,
                    Term = commandEntry1_LeaderCommitted.Term
                }, CancellationToken.None);

                writingEntries.Wait(EventNotificationTimeOut);

                commitIndexUpdated.Wait(EventNotificationTimeOut);
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            var afterAppendEntries = CaptureStateProperties();

            var lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            writingEntries
                .IsConditionMatched
                .Should().BeTrue("Concerned activity should be logged");

            lastEntry
                .CurrentIndex
                .Should().Be(commandEntry2_LeaderUnCommitted.CurrentIndex, $"As additional command entries have been appended, and {nameof(commandEntry2_LeaderUnCommitted)} is the last one");

            lastEntry
                .Term
                .Should().Be(commandEntry2_LeaderUnCommitted.Term, "As the term should be same as before");

            lastEntry
                .Type
                .Should().Be(LogEntry.Types.Command, "As the appended entry must be a command entry");

            afterAppendEntries
                .CurrentTerm
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case");

            afterAppendEntries
                .StateValue
                .Should().Be(StateValues.Follower, "As it should still be a follower");

            afterAppendEntries
                .VotedFor
                .Should().Be(MockNodeA, "As the vote should still be granted to the previous sender");

            afterAppendEntries
                .LastApplied
                .Should().Be(commandEntry1_LeaderCommitted.CurrentIndex, $"As the leader commit index sent is not pointing to {commandEntry2_LeaderUnCommitted.CurrentIndex}, the follower can update its own commit index as new entries ARE ALSO being sent. Therefore, it should update maximum upto the leader commit index which would be consisting of the command entry {nameof(commandEntry1_LeaderCommitted)}");

            afterAppendEntries
                .CommitIndex
                .Should().Be(commandEntry1_LeaderCommitted.CurrentIndex, $"As the leader commit index sent is not pointing to {commandEntry2_LeaderUnCommitted.CurrentIndex}, the follower can update its own commit index as new entries ARE ALSO being sent. Therefore, it should update maximum upto the leader commit index which would be consisting of the command entry {nameof(commandEntry1_LeaderCommitted)}");

            appendEntriesResponse
                .IsSuccessful
                .Should().BeTrue("As a RPC response is expected without a failure");

            appendEntriesResponse
                .Response.Term
                .Should().Be(termBeforeRPC, $"As it should not update the term, since the term is already updated from the prev test case {IsFollowerUpdatingTermToHigherReceivedValue}");

            appendEntriesResponse
                .Response.Success
                .Should().BeTrue("As acknowledgement should be successful");

            appendEntriesResponse
               .Response.ConflictingEntryTermOnFailure
               .Should().BeNull("As there should be no conflicts");

            appendEntriesResponse
               .Response.FirstIndexOfConflictingEntryTermOnFailure
               .Should().BeNull("As there should be no conflicts");

            Context.GetService<INoteStorage>().HasNote(newNote)
                .Should().BeTrue($"As the command {commandEntry1_LeaderCommitted} should have been applied, and this note should be avaiable, since the LeaderCommitIndex supplied is accomodating the corresponding log entry");

            Context.GetService<INoteStorage>().HasNote(newNote2)
                .Should().BeFalse($"As the command {commandEntry2_LeaderUnCommitted} should not have been applied, and this note should not be avaiable as the corresponding log entry was not committed by leader yet, and thus sohuld not have been applied to the follower state machine");

            Cleanup();
            #endregion
        }
    }
}
