﻿using ActivityLogger.Logging;
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

namespace Coracle.IntegrationTests.Framework
{
    [TestCaseOrderer($"Coracle.IntegrationTests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.IntegrationTests")]
    public class CandidacyAndLeaderTest : BaseTest, IClassFixture<TestContext>
    {
        public CandidacyAndLeaderTest(TestContext context) : base(context)
        {
        }

        [Fact]
        [Order(1)]
        public void IsInitializationDoneProperly()
        {
            //Arrange
            Exception caughtException = null;

            //Act
            try
            {
                InitializeEngineConfiguration();
                InitializeNode();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

#region Assertions
            caughtException
                .Should().Be(null, $"- Initialization should occur successfully and not throw {caughtException}");

            Context
                .GetService<IEngineConfiguration>()
                .NodeId
                .Should().BeEquivalentTo(SUT, $"- supplied value in the EngineConfigurationSettings.NodeId is {SUT}");

            Context
                .GetService<IDiscoverer>()
                .GetAllNodes(Context.GetService<IEngineConfiguration>().DiscoveryServerUri, CancellationToken.None)
                .GetAwaiter().GetResult().AllNodes.First().UniqueNodeId
                .Should().BeEquivalentTo(SUT, $"- the discoverer holding NodeRegistrar should enroll this current node");

            Context
                .GetService<ITaskProcessorConfiguration>()
                .ProcessorQueueSize
                .Should().Be(DecidedEventProcessorQueueSize, "- during Initialization, the EventProcessorConfiguration should be updated to the QueueSize supplied");

            Context
                .GetService<ITaskProcessorConfiguration>()
                .ProcessorWaitTimeWhenQueueEmpty_InMilliseconds
                .Should().Be(DecidedEventProcessorWait, "- during Initialization, the EventProcessorConfiguration should be updated to the WaitTime supplied");

            Context
                .GetService<IClusterConfiguration>()
                .Peers.Count()
                .Should().Be(0, "- no external nodes have been enrolled yet apart from this node");

            Context
                .GetService<IClusterConfiguration>()
                .ThisNode.UniqueNodeId
                .Should().BeEquivalentTo(SUT, "- only the current node has been registered");

            Context
                .GetService<ICanoeNode>()
                .IsInitialized
                .Should().BeTrue("- post Initialzation the flag should be marked true");
#endregion
        }

        [Fact]
        [Order(2)]
        public Task IsReInitializationWorking()
        {
#region Arrange

            CreateMockNode(MockNodeA);
            CreateMockNode(MockNodeB);

#endregion

#region Act

            Exception caughtException = null;

            try
            {
                RegisterMockNodeInRegistrar(MockNodeA);
                RegisterMockNodeInRegistrar(MockNodeB);

                InitializeNode();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

#endregion

#region Assert

            caughtException
                .Should().Be(null, $"- RefreshDiscovery should not throw {caughtException}");

            Context
                .GetService<IDiscoverer>()
                .GetAllNodes(Context.GetService<IEngineConfiguration>().DiscoveryServerUri, CancellationToken.None)
                .GetAwaiter().GetResult().AllNodes
                .Select(x=> x.UniqueNodeId)
                .Should().Contain(new string[] { MockNodeA, MockNodeB }, $"- the discoverer holding NodeRegistrar should enroll both mock nodes");

            Context
                .GetService<IClusterConfiguration>()
                .Peers.Count()
                .Should().Be(2, "- 2 Mock Nodes have been enrolled apart from this node");

            Context
                .GetService<IClusterConfiguration>()
                .Peers
                .Select(x => x.UniqueNodeId)
                .Should().Contain(expected: new string[] { MockNodeA, MockNodeB }, because: "- 2 Mock Nodes have been enrolled apart from this node");

            Context
                .GetService<IClusterConfiguration>()
                .ThisNode.UniqueNodeId
                .Should().BeEquivalentTo(SUT, "- ThisNode details should not change");

            return Task.CompletedTask;
#endregion
        }


        [Fact]
        [Order(3)]
        public void IsNodeStartSuccessful()
        {
            #region Arrange

            Context.GetService<IActivityMonitor<Activity>>().Start();

            var queue = CaptureActivities();

#endregion

#region Act

            Exception caughtException = null;

            try
            {
                StartNode();
            }
            catch(Exception ex)
            {
                caughtException = ex;
            }

            var assertableQueue = StartAssertions(queue);

#endregion

#region Assert

            caughtException
                .Should().Be(null, $"Node should be started successfully");

            Context
                .GetService<IResponsibilities>()
                .UniqueIdentifier
                .Should().Be(SUT, $"Since Responsibilities are configured for {SUT}");

            Context
                .GetService<IResponsibilities>()
                .GlobalCancellationToken
                .IsCancellationRequested
                .Should().BeFalse($"Since the State has just begun");

            Context
                .GetService<ICurrentStateAccessor>()
                .Get()
                .StateValue
                .Should().Be(StateValues.Follower, $"Initial state should be Follower when the node is started");

            Context
                .GetService<ICurrentStateAccessor>()
                .Get()
                .VolatileState
                .Should().Match<IVolatileProperties>(volatileState => volatileState.LastApplied == 0 && volatileState.CommitIndex == 0, $"All volatile properties should be 0 at the start");

            assertableQueue
                .Search()
                .UntilItSatisfies(x => x.Event.Equals(TaskProcessingEngine.Starting) 
                    && x.EntitySubject.Equals(TaskProcessingEngine.ProcessorEntity), $"Event Processor should start and log an Activity");

            Cleanup();
#endregion
        }


        [Fact]
        [Order(4)]
        public async Task VerifyWaitForCommandAsNoLeaderRecognizedInitially()
        {
            #region Arrange

            var (Command, Note) = TestAddCommand();

            var queue = CaptureActivities();

#endregion

#region Act

            Exception caughtException = null;
            ClientHandlingResult clientHandlingResult = null;

            try
            {
                clientHandlingResult = await Context.GetService<IExternalClientCommandHandler>()
                    .HandleClientCommand(Command, CancellationToken.None);
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assert

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Exceptions should never bubble up and impact calling context");

            clientHandlingResult
                .Should().Match<ClientHandlingResult>(predicate: x => x.LeaderNodeConfiguration == null || x.LeaderNodeConfiguration.UniqueNodeId == null, $"Leader Node should not be marked as ElectionTimer is awaited");

            clientHandlingResult
                .Exception
                .Should().BeOfType<TimeoutException>($"finding Leader Node within the Client Timeout was bound to fail as Election Timer is awaited");

            clientHandlingResult
                .IsOperationSuccessful
                .Should().BeFalse($"As Command was not executed successfully");

            assertableQueue
                .Search()
                .UntilItSatisfies(x => x.Event.Equals($"{OnClientCommandReceive<AddNoteCommand>.RetryingAsLeaderNodeNotFound}"), $"Event Processor should start and log an Activity");

            Cleanup();
#endregion
        }

        [Fact]
        [Order(5)]
        public async Task IsFollowerTurningToCandidateAndThenToLeader()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var candidateEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange)
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Candidate))).RemoveOnceMatched();

            var termChanged = notifiableQueue
                .AttachNotifier(x => x.Is(TestStateProperties.Entity, TestStateProperties.IncrementedCurrentTerm));

            var remoteCallMade = notifiableQueue
                .AttachNotifier(x => x.Is(TestRemoteManager.TestRemoteManagerEntity, TestRemoteManager.OutboundRequestVoteRPC));

            var sessionReceiveVote = notifiableQueue
                .AttachNotifier(x => x.Is(ElectionManager.Entity, ElectionManager.ReceivedVote));

            var majorityAttained = notifiableQueue
                .AttachNotifier(x => x.Is(ElectionManager.Entity, ElectionManager.MajorityAttained));

            var leaderEstablished = notifiableQueue
                .AttachNotifier(x => 
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange) 
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Leader))).RemoveOnceMatched();

            var updatedIndices = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(LeaderVolatileProperties.Entity, LeaderVolatileProperties.UpdatedIndices));

            var commitIndexUpdated = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(AbstractState.Entity, AbstractState.ApplyingLogEntry));

            EnqueueRequestVoteSuccessResponse(MockNodeA);
            EnqueueRequestVoteSuccessResponse(MockNodeB);

            var (Command, Note) = TestAddCommand();

            #endregion

            #region Act

            Exception caughtException = null;
            ClientHandlingResult clientHandlingResult = null;

            StateCapture 
                captureAfterCandidacy = null, 
                captureJustAfterLeaderStateChange = null, 
                captureAfterCommand = null, 
                captureBeforeCandidacy = null, 
                captureAfterSuccessfulAppendEntries = null;

            try
            {
                captureBeforeCandidacy = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());

                var electionTimer = Context.GetService<IElectionTimer>() as TestElectionTimer;

                //This will make sure that the ElectionTimer callback invocation is approved for the Candidacy
                electionTimer.AwaitedLock.ApproveNext();

                //Wait until current state is Candidate
                candidateEstablished.Wait(EventNotificationTimeOut);

                ////Wait until Term incremented
                termChanged.Wait(EventNotificationTimeOut);

                ////Wait until TestRemoteManager receives a call
                remoteCallMade.Wait(EventNotificationTimeOut);

                captureAfterCandidacy = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());

                ////Wait until ElectionSession receives a vote
                sessionReceiveVote.Wait(EventNotificationTimeOut);

                ////Wait until Majority has been attained
                majorityAttained.Wait(EventNotificationTimeOut);


                var heartBeatTimer = Context.GetService<IHeartbeatTimer>() as TestHeartbeatTimer;

                EnqueueAppendEntriesSuccessResponse(MockNodeA);
                EnqueueAppendEntriesSuccessResponse(MockNodeB);

                //This will make sure that the Heartbeat callback invocation is approved for SendAppendEntries
                heartBeatTimer.AwaitedLock.ApproveNext();

                //Wait until current state is now Leader
                leaderEstablished.Wait(EventNotificationTimeOut);

                captureJustAfterLeaderStateChange = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());

                var isPronouncedLeaderSelf = Context.GetService<ILeaderNodePronouncer>().IsLeaderRecognized 
                    && Context.GetService<ILeaderNodePronouncer>().RecognizedLeaderConfiguration.UniqueNodeId.Equals(SUT);

                updatedIndices.Wait(EventNotificationTimeOut);
                commitIndexUpdated.Wait(EventNotificationTimeOut);

                captureAfterSuccessfulAppendEntries = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());

                EnqueueAppendEntriesSuccessResponse(MockNodeA);
                EnqueueAppendEntriesSuccessResponse(MockNodeB);

                clientHandlingResult = await Context.GetService<IExternalClientCommandHandler>()
                    .HandleClientCommand(Command, CancellationToken.None);

                updatedIndices.Wait(EventNotificationTimeOut);
                commitIndexUpdated.Wait(EventNotificationTimeOut);

                captureAfterCommand = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());
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

            candidateEstablished.IsConditionMatched.Should().BeTrue();
            termChanged.IsConditionMatched.Should().BeTrue();
            remoteCallMade.IsConditionMatched.Should().BeTrue();
            sessionReceiveVote.IsConditionMatched.Should().BeTrue();
            majorityAttained.IsConditionMatched.Should().BeTrue();
            leaderEstablished.IsConditionMatched.Should().BeTrue();

            captureAfterCandidacy
                .VotedFor
                .Should().Be(SUT, "Because tduring candidacy, the candidate votes for itself");

            captureAfterCandidacy
                .CurrentTerm
                .Should().Be(captureBeforeCandidacy.CurrentTerm + 1, "Since before candidacy, the term should be 1 lesser");





            captureJustAfterLeaderStateChange
                .CommitIndex
                .Should().Be(0, "CommitIndex should be zero, since leader has just been established, and NoOp entry not sent out yet/currenty being sent out");

            captureJustAfterLeaderStateChange
                .CurrentTerm
                .Should().Be(captureAfterCandidacy.CurrentTerm, "Since Leader was elected in the candidacy election itself");





            captureAfterSuccessfulAppendEntries
                .CommitIndex
                .Should().Be(1, "CommitIndex should be 1, since leader has been established, and NoOp entry has been replicated to majority, thus has been committed");

            captureAfterSuccessfulAppendEntries
                .LastApplied
                .Should().Be(1, "LastApplied entry should be the same as CommitIndex and should be 1");

            captureAfterSuccessfulAppendEntries
                .NextIndexes
                .Values
                .Should()
                .Match(i => i.All(_ => _.Equals(2)),
                    $"{MockNodeA} and {MockNodeB} should have had replicated the NoOp entry, i.e up until the leader's last log index, and the nextIndex to send for each peer mock node should be one greater, i.e 2");





            Context.GetService<INotes>()
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
        [Order(6)]
        public async Task IsLeaderHandlingReadOnlyCommands()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var noteHeader = Context.GetService<INotes>().GetAllHeaders().First();

            Context.GetService<INotes>().TryGet(noteHeader, out var existingNote);

            var command = TestGetCommand(noteHeader);

            #endregion

            #region Act

            Exception caughtException = null;
            ClientHandlingResult clientHandlingResult = null;
            StateCapture captureAfterReadOnlyCommand = null, captureBeforeReadOnlyCommand = null;
            try
            {
                captureBeforeReadOnlyCommand = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());

                EnqueueAppendEntriesSuccessResponse(MockNodeA);
                EnqueueAppendEntriesSuccessResponse(MockNodeB);

                clientHandlingResult = await Context.GetService<IExternalClientCommandHandler>()
                    .HandleClientCommand(command, CancellationToken.None);

                captureAfterReadOnlyCommand = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());
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

            clientHandlingResult.CommandResult.Should().NotBeNull("Result must have the Note, since it exists");

            clientHandlingResult.CommandResult.As<Note>().Text.Should().Be(existingNote.Text, "Note should match the existing object's text");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(AppendEntriesManager.Entity, AppendEntriesManager.Initiate), "As client command processing should invoke a heartbeat to check if the state is still Leader, without the Heartbeat timer initiating");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(GlobalAwaiter.Entity, GlobalAwaiter.AwaitingNoDeposition), "Node must check if it has been deposed before responding to read-only requests");


            captureBeforeReadOnlyCommand
                .CommitIndex
                .Should().Be(captureAfterReadOnlyCommand.CommitIndex, "Since Read-only request, the CommitIndex should remain same since nothing should be applied");

            captureBeforeReadOnlyCommand
                .MatchIndexes[MockNodeA]
                .Should()
                .Be(captureAfterReadOnlyCommand.MatchIndexes[MockNodeA], "Since a Read-Only Command does not append Entries, the MatchIndex must remain the same as before");

            captureBeforeReadOnlyCommand
                .MatchIndexes[MockNodeB]
                .Should()
                .Be(captureAfterReadOnlyCommand.MatchIndexes[MockNodeB], "Since a Read-Only Command does not append Entries, the MatchIndex must remain the same as before");

            Cleanup();
            #endregion
        }

        [Fact]
        [Order(7)]
        public async Task IsLeaderDenyingAppendEntriesAndRequestVote()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var previousTerm = await Context.GetService<IPersistentProperties>().GetCurrentTerm() - 1;
            var lastLogIndexOfPrevTerm = await Context.GetService<IPersistentProperties>().LogEntries.GetLastIndexForTerm(previousTerm);
            var incompleteLogEntries = await Context.GetService<IPersistentProperties>().LogEntries.FetchLogEntriesBetween(0, lastLogIndexOfPrevTerm);

            #endregion

            #region Act

            Exception caughtException = null;
            Operation<IRequestVoteRPCResponse> requestVoteResponse = null;
            Operation<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            try
            {
                requestVoteResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new RequestVoteRPC()
                {
                    CandidateId = MockNodeA,
                    LastLogIndex = lastLogIndexOfPrevTerm,
                    LastLogTerm = previousTerm,
                    Term = previousTerm
                }, CancellationToken.None);

                appendEntriesResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new AppendEntriesRPC(incompleteLogEntries)
                {
                    LeaderId = MockNodeA,
                    LeaderCommitIndex = lastLogIndexOfPrevTerm,
                    Term = previousTerm,
                    PreviousLogIndex = incompleteLogEntries.Last().CurrentIndex,
                    PreviousLogTerm = incompleteLogEntries.Last().Term,
                }, CancellationToken.None);
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

            requestVoteResponse
                .HasResponse
                .Should().BeTrue("There must be some response");

            requestVoteResponse
                .Response
                .VoteGranted
                .Should().BeFalse("Vote must not be granted, as the Term sent is earlier");

            requestVoteResponse
                .Response
                .Term
                .Should().BeGreaterThan(previousTerm, "Since the term sent back is greater");

            appendEntriesResponse
                .HasResponse
                .Should().BeTrue("There must be some response");

            appendEntriesResponse
                .Response
                .Success
                .Should().BeFalse("Since entries do not overlap already present entries");

            appendEntriesResponse
                .Response
                .Term
                .Should().BeGreaterThan(previousTerm, "Since the term sent back is greater");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnExternalRequestVoteRPCReceive.ActionName, OnExternalRequestVoteRPCReceive.DeniedDueToLesserTerm), "The denial should be stemmed from this event");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.DeniedDueToNonExistentPreviousIndex), "The denial should be stemmed from this event");

            Cleanup();
            #endregion
        }

        [Fact]
        [Order(8)]
        public async Task IsLeaderHandlingConfigurationChange()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var candidateEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange)
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Candidate))).RemoveOnceMatched();

            var currentConfiguration = Context.GetService<IClusterConfiguration>().CurrentConfiguration;

            var newConfiguration = currentConfiguration.Select(x => new NodeConfiguration
            {
                UniqueNodeId = x.UniqueNodeId

            }).Append(new NodeConfiguration
            {
                UniqueNodeId = MockNewNodeC //Adding NewNodeC

            }).Where(x=> !x.UniqueNodeId.Equals(MockNodeB)) //Removing MockNodeIdB
            .ToList();

            CreateMockNode(MockNewNodeC);

            #endregion

            #region Act

            Exception caughtException = null;
            ConfigurationChangeResult changeResult = null;

            try
            {
                EnqueueAppendEntriesSuccessResponse(MockNodeA);
                EnqueueAppendEntriesSuccessResponse(MockNodeB);
                
                // New Node may respond with Success false, as it has just started up and needs replication
                Context.NodeContext.GetMockNode(MockNewNodeC).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = rpc.PreviousLogIndex,
                    ConflictingEntryTermOnFailure = rpc.PreviousLogTerm,
                    Success = false
                }, approveImmediately: true);

                // Next time, it sends true
                EnqueueAppendEntriesSuccessResponse(MockNewNodeC);

                changeResult = await Context.GetService<IExternalConfigurationChangeHandler>().IssueConfigurationChange(new ConfigurationChangeRPC
                {
                    UniqueId = Guid.NewGuid().ToString(),
                    NewConfiguration = newConfiguration

                }, CancellationToken.None);
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

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(ClusterConfiguration.Entity, ClusterConfiguration.NewUpdate)
                    && _.HasMatchingParam(ClusterConfiguration.allNodeIds, param => param.ToString().ContainsThese(SUT, MockNodeA, MockNodeB, MockNewNodeC)),
                        $"Joint Consensus should account for the new {MockNewNodeC} and all other existing nodes");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(AppendEntriesManager.Entity, AppendEntriesManager.NewConfigurationManagement) 
                    && _.HasMatchingParam(AppendEntriesManager.nodesToAdd, param => param.ToString().Contains(MockNewNodeC) && param.ToString().DoesNotContainThese(MockNodeA, MockNodeB, SUT))
                        && _.HasMatchingParam(AppendEntriesManager.nodesToRemove, param => param.ToString().DoesNotContainThese(MockNodeA, MockNodeB, SUT, MockNewNodeC)), 
                        $"Joint Consensus should account for the new {MockNewNodeC} only, but should not remove {MockNodeB} or any other node");

            assertableQueue
                .Search()
                .UntilItSatisfies(_ => _.Is(LeaderVolatileProperties.Entity, LeaderVolatileProperties.DecrementedNextIndexToFirstIndexOfPriorValidTerm)
                    && _.HasMatchingParam(LeaderVolatileProperties.nodeId, param => param.ToString().ContainsThese(MockNewNodeC)),
                        $"Since entries are not yet replicated to the {MockNewNodeC}, the NextIndex must be decremented accordingly");

            assertableQueue
                .Search()
                .UntilItSatisfies(_ => _.Is(OnCatchUpOfNewlyAddedNodes.ActionName, OnCatchUpOfNewlyAddedNodes.NodesNotCaughtUpYet)
                    && _.HasMatchingParam(OnCatchUpOfNewlyAddedNodes.nodesToCheck, param => param.ToString().ContainsThese(MockNewNodeC)
                            && param.ToString().DoesNotContainThese(MockNodeA, MockNodeB, SUT)),
                        $"Catchup awaiting should only occur for {MockNewNodeC}, and not other nodes");

            assertableQueue
                .Search()
                .UntilItSatisfies(_ => _.Is(OnSendAppendEntriesRPC.ActionName, OnSendAppendEntriesRPC.SendingOnFailure),
                        $"Catchup awaiting should only occur for {MockNewNodeC}, and not other nodes");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnCatchUpOfNewlyAddedNodes.ActionName, OnCatchUpOfNewlyAddedNodes.NodesCaughtUp)
                    && _.HasMatchingParam(OnCatchUpOfNewlyAddedNodes.nodesToCheck, param => param.ToString().ContainsThese(MockNewNodeC) 
                            && param.ToString().DoesNotContainThese(MockNodeA, MockNodeB, SUT)),
                        $"Catchup awaiting should only occur for {MockNewNodeC}, and not other nodes");

            Context.NodeContext.GetMockNode(MockNewNodeC).AppendEntriesLock
                .RemoteCalls.Last().input.Entries.Last().Contents.As<IEnumerable<NodeConfiguration>>()
                    .Should().Match(configs => string.Join(' ', configs.Select(x => x.UniqueNodeId))
                        .ContainsThese(SUT, MockNodeA, MockNodeB, MockNewNodeC), 
                            $"All node Ids must be present in the Joint Consensus Configuration entry sent to the new Node as well ");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(ClusterConfiguration.Entity, ClusterConfiguration.NewUpdate)
                    && _.HasMatchingParam(ClusterConfiguration.allNodeIds, param => param.ToString().ContainsThese(SUT, MockNodeA, MockNewNodeC) 
                        && param.ToString().DoesNotContainThese(MockNodeB)),
                        $"Target Configuration should have the new {MockNewNodeC} and all other existing nodes except {MockNodeB}");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(AppendEntriesManager.Entity, AppendEntriesManager.NewConfigurationManagement)
                    && _.HasMatchingParam(AppendEntriesManager.nodesToAdd, param => param.ToString().DoesNotContainThese(MockNodeA, MockNodeB, SUT, MockNewNodeC))
                        && _.HasMatchingParam(AppendEntriesManager.nodesToRemove, param => param.ToString().Contains(MockNodeB) && param.ToString().DoesNotContainThese(MockNodeA, SUT, MockNewNodeC)),
                        $"When the Configuration Change is handled again by a downstream module, they will just remove {MockNodeB}. ");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnConfigurationChangeRequestReceive.ActionName, OnConfigurationChangeRequestReceive.ConfigurationChangeSuccessful),
                        $"New Target Configuration Log Entry should be successfully changed");

            Cleanup();
            #endregion
        }

        [Fact]
        [Order(9)]
        public async Task IsLeaderChangingToFollowerForGreaterTermRPC()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var termChanged = notifiableQueue
                .AttachNotifier(x => x.Is(TestStateProperties.Entity, TestStateProperties.SetCurrentTermExternally));

            var followerEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange)
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Follower))).RemoveOnceMatched();

            var currentTerm = await Context.GetService<IPersistentProperties>().GetCurrentTerm();
            var lastLogIndexOfCurrentTerm = await Context.GetService<IPersistentProperties>().LogEntries.GetLastIndex();

            #endregion

            #region Act

            Exception caughtException = null;
            Operation<IRequestVoteRPCResponse> requestVoteResponse = null;

            try
            {
                requestVoteResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new RequestVoteRPC()
                {
                    CandidateId = MockNodeA,
                    LastLogIndex = lastLogIndexOfCurrentTerm + 1,
                    LastLogTerm = currentTerm,
                    Term = currentTerm + 1
                }, CancellationToken.None);

                termChanged.Wait(EventNotificationTimeOut);

                followerEstablished.Wait(EventNotificationTimeOut);
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

            termChanged.IsConditionMatched.Should().BeTrue();

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnExternalRequestVoteRPCReceive.ActionName, OnExternalRequestVoteRPCReceive.BeingFollowerAsGreaterTermReceived), "Leader should become a follower, as it has encountered a greater term");

            Context.GetService<IPersistentProperties>().GetCurrentTerm().GetAwaiter().GetResult()
                .Should()
                .BeGreaterThan(currentTerm, "As Term must be set to the bigger Term (if encountered) from Request/Response");

            followerEstablished.IsConditionMatched.Should().BeTrue();

            Cleanup();
            #endregion
        }
    }
}