using ActivityLogger.Logging;
using Coracle.Samples.ClientHandling.Notes;
using Coracle.IntegrationTests.Components.PersistentData;
using Coracle.IntegrationTests.Components.Remoting;
using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.Discovery.Registrar;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;
using EntityMonitoring.FluentAssertions.Extensions;
using EntityMonitoring.FluentAssertions.Structure;
using FluentAssertions;
using TaskGuidance.BackgroundProcessing.Core;
using Xunit;
using Coracle.Samples.ClientHandling.NoteCommand;
using Coracle.Raft.Engine.Logs;

namespace Coracle.IntegrationTests.Framework
{
    [TestCaseOrderer($"Coracle.IntegrationTests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.IntegrationTests")]
    public class FollowerTest : BaseTest, IClassFixture<TestContext>
    {
        public FollowerTest(TestContext context) : base(context)
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
            
            Operation<IRequestVoteRPCResponse> requestVoteResponse = null;

            Context.GetService<IActivityMonitor<Activity>>().Start();

            var queue = CaptureActivities();

            var greaterTermEncountered = queue
                .AttachNotifier(_ => _.Is(OnExternalRequestVoteRPCReceive.ActionName, OnExternalRequestVoteRPCReceive.BeingFollowerAsGreaterTermReceived))
                .RemoveOnceMatched();

#endregion

#region Act

            var term = await Context.GetService<IPersistentProperties>().GetCurrentTerm();
            var entry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

            try
            {
                requestVoteResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new RequestVoteRPC()
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
                .Should().Be(Raft.Engine.Logs.LogEntry.Types.None, "As this is the initialized entry type");

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
                .HasResponse
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

            Operation<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var writingEntries = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.OverwritingEntriesIfAny))
                .RemoveOnceMatched();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentProperties>().GetCurrentTerm();

            LogEntry senderLeaderlogEntry = new LogEntry
            {
                CurrentIndex = 1,
                Type = LogEntry.Types.NoOperation,
                Term = 1,
                Content = null
            };

            try
            {
                appendEntriesResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new AppendEntriesRPC(entries: new List<LogEntry>
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

            var lastEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

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
                .HasResponse
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

            Operation<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var acknowledgedHeartbeat = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.Acknowledged))
                .RemoveOnceMatched();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentProperties>().GetCurrentTerm();

            try
            {
                appendEntriesResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new AppendEntriesRPC(entries: null)
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

            var lastEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

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
                .HasResponse
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

            Operation<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var writingEntries = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.OverwritingEntriesIfAny))
                .RemoveOnceMatched();

            var (command, note) = TestAddCommand();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentProperties>().GetCurrentTerm();

            var commandLogEntry = new LogEntry
            {
                CurrentIndex = 2,
                Term = 1,
                Type = LogEntry.Types.Command,
                Content = command
            };

            try
            {
                appendEntriesResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new AppendEntriesRPC(entries: new List<LogEntry>
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

            var lastEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

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
                .HasResponse
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

            Operation<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var acknowledgedHeartbeat = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.Acknowledged))
                .RemoveOnceMatched();

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentProperties>().GetCurrentTerm();

            try
            {
                appendEntriesResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new AppendEntriesRPC(entries: null)
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

            var lastEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

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
                .HasResponse
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

            Operation<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            var queue = CaptureActivities();

            var writingEntries = queue
                .AttachNotifier(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.OverwritingEntriesIfAny))
                .RemoveOnceMatched();

            var commitIndexUpdated = queue
                .AttachNotifier(_ => _.Is(AbstractState.Entity, AbstractState.ApplyingLogEntry))
                .RemoveOnceMatched();

            var lastCommandCounter = Context.CommandContext.GetLastCommandCounter;

            var (newCommand, newNote) = TestAddCommand();

            var previouslyAddedCommandEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtIndex(2);

            var previouslyAddedCommand = await Context.GetService<IPersistentProperties>().LogEntries.ReadFrom<NoteCommand>(previouslyAddedCommandEntry);

            #endregion

            #region Act

            var termBeforeRPC = await Context.GetService<IPersistentProperties>().GetCurrentTerm();

            var commandLogEntry = new LogEntry
            {
                CurrentIndex = 3,
                Term = 1,
                Type = LogEntry.Types.Command,
                Content = newCommand
            };

            try
            {
                appendEntriesResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new AppendEntriesRPC(entries: new List<LogEntry>
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

            var lastEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

            bool isPreviousNoteCommandExecuted = Context.GetService<INotes>().TryGet(GetNoteHeader(lastCommandCounter), out var previousNote);

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
                .HasResponse
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
    }
}
