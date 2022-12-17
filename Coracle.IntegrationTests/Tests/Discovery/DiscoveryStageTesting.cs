using ActivityLogger.Logging;
using ActivityMonitoring.Assertions.Core;
using Coracle.IntegrationTests.Components.ClientHandling.NoteCommand;
using Coracle.IntegrationTests.Components.ClientHandling.Notes;
using Coracle.IntegrationTests.Components.PersistentData;
using Coracle.IntegrationTests.Components.Remoting;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Discovery.Registrar;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Canoe.Engine.States.LeaderState;
using EventGuidance.Logging;
using EventGuidance.Responsibilities;
using FluentAssertions;
using Xunit;


namespace Coracle.IntegrationTests.Framework
{
    [TestCaseOrderer($"Coracle.IntegrationTests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.IntegrationTests")]
    public class DiscoveryStageTesting : IClassFixture<TestContext>
    {
        const string TestingNodeId = "SUT";
        const string MockNodeIdA = "MockA";
        const string MockNodeIdB = "MockB";
        const string NewNodeC = "NewNodeA";

        const int Seconds = 1000;
        const int MilliSeconds = 1;
        const int DecidedEventProcessorQueueSize = 49;
        const int DecidedEventProcessorWait = 5 * Seconds;
        public EngineConfigurationSettings TestEngineSettings => new EngineConfigurationSettings
        {
            NodeId = TestingNodeId,
            ThisNodeUri = null,
            DiscoveryServerUri = null,
            IncludeOriginalClientCommandInResults = true,
            WaitPostEnroll_InMilliseconds = 0,
            CommandHandlingEndpoint = null,
            EventProcessorQueueSize = DecidedEventProcessorQueueSize,
            EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds = DecidedEventProcessorWait,

            NoLeaderElectedWaitInterval_InMilliseconds = 10000 * MilliSeconds,
#if DEBUG
            ClientCommandTimeout_InMilliseconds = 1 * Seconds,
#else
            ClientCommandTimeout_InMilliseconds = 500 * MilliSeconds,
#endif
            HeartbeatInterval_InMilliseconds = 5 * Seconds,
            SendAppendEntriesRPC_MaxSessionCapacity = 10,

            MinElectionTimeout_InMilliseconds = 19 * Seconds,
            MaxElectionTimeout_InMilliseconds = 20 * Seconds,

            //AppendEntries
            AppendEntriesEndpoint = null,
            AppendEntriesTimeoutOnSend_InMilliseconds = 1000 * Seconds,
            AppendEntriesTimeoutOnReceive_InMilliseconds = 1000 * Seconds,
            SendAppendEntriesRPC_MaxRetryInfinityCounter = 20,
                
            //RequestVote
            RequestVoteEndpoint = null,
            RequestVoteTimeoutOnSend_InMilliseconds = 1000 * Seconds,
            RequestVoteTimeoutOnReceive_InMilliseconds = 1000 * Seconds,
            SendRequestVoteRPC_MaxRetryInfinityCounter = 20,

            IncludeConfigurationChangeRequestInResults = true,
            IncludeJointConsensusConfigurationInResults = true,
            IncludeOriginalConfigurationInResults = true,

            CatchUpOfNewNodesTimeout_InMilliseconds = 100 * Seconds,
            CatchUpOfNewNodesWaitInterval_InMilliseconds = 10 * Seconds,

            CatchupIntervalOnConfigurationChange_InMilliseconds = 1 * MilliSeconds,
            CheckDepositionWaitInterval_InMilliseconds = 2 * Seconds,

            EntryCommitWaitInterval_InMilliseconds = 2 * Seconds,
            EntryCommitWaitTimeout_InMilliseconds = 1000 * Seconds
        };
        
        public DiscoveryStageTesting(TestContext context)
        {
            Context = context;
        }

        public TestContext Context { get; }

        [Fact]
        [Order(1)]
        public void IsInitializationDoneProperly()
        {

            //Arrange

            Exception caughtException = null;

            //Act
            try
            {
                (Context.GetService<IEngineConfiguration>() as EngineConfigurationSettings).ApplyFrom(TestEngineSettings);
                Context.GetService<ICanoeNode>().InitializeConfiguration();
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
                .Should().BeEquivalentTo(TestingNodeId, $"- supplied value in the EngineConfigurationSettings.NodeId is {TestingNodeId}");

            Context
                .GetService<IDiscoverer>()
                .GetAllNodes(Context.GetService<IEngineConfiguration>().DiscoveryServerUri, CancellationToken.None)
                .GetAwaiter().GetResult().AllNodes.First().UniqueNodeId
                .Should().BeEquivalentTo(TestingNodeId, $"- the discoverer holding NodeRegistrar should enroll this current node");

            Context
                .GetService<IEventProcessorConfiguration>()
                .EventProcessorQueueSize
                .Should().Be(DecidedEventProcessorQueueSize, "- during Initialization, the EventProcessorConfiguration should be updated to the QueueSize supplied");

            Context
                .GetService<IEventProcessorConfiguration>()
                .EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds
                .Should().Be(DecidedEventProcessorWait, "- during Initialization, the EventProcessorConfiguration should be updated to the WaitTime supplied");

            Context
                .GetService<IClusterConfiguration>()
                .Peers.Count()
                .Should().Be(0, "- no external nodes have been enrolled yet apart from this node");

            Context
                .GetService<IClusterConfiguration>()
                .ThisNode.UniqueNodeId
                .Should().BeEquivalentTo(TestingNodeId, "- only the current node has been registered");

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

            Context.NodeContext.CreateMockNode(MockNodeIdA);
            Context.NodeContext.CreateMockNode(MockNodeIdB);

#endregion

#region Act

            Exception caughtException = null;

            try
            {
                Context.GetService<INodeRegistrar>().Enroll(new NodeConfiguration
                {
                    BaseUri = null,
                    UniqueNodeId = MockNodeIdA
                }, CancellationToken.None);

                Context.GetService<INodeRegistrar>().Enroll(new NodeConfiguration
                {
                    BaseUri = null,
                    UniqueNodeId = MockNodeIdB
                }, CancellationToken.None);

                Context.GetService<ICanoeNode>().InitializeConfiguration();
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
                .Should().Contain(new string[] { MockNodeIdA, MockNodeIdB }, $"- the discoverer holding NodeRegistrar should enroll both mock nodes");

            Context
                .GetService<IClusterConfiguration>()
                .Peers.Count()
                .Should().Be(2, "- 2 Mock Nodes have been enrolled apart from this node");

            Context
                .GetService<IClusterConfiguration>()
                .Peers
                .Select(x => x.UniqueNodeId)
                .Should().Contain(expected: new string[] { MockNodeIdA, MockNodeIdB }, because: "- 2 Mock Nodes have been enrolled apart from this node");

            Context
                .GetService<IClusterConfiguration>()
                .ThisNode.UniqueNodeId
                .Should().BeEquivalentTo(TestingNodeId, "- ThisNode details should not change");

            return Task.CompletedTask;
#endregion
        }


        [Fact]
        [Order(3)]
        public void IsNodeStartSuccessful()
        {
#region Arrange

            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            monitor.Start();

            var contextQueueId = monitor.Capture().All().Id;

            IAssertableQueue<Activity> assertableQueue;

#endregion

#region Act

            Exception caughtException = null;

            try
            {
                var node = Context.GetService<ICanoeNode>();
                node.Start();
            }
            catch(Exception ex)
            {
                caughtException = ex;
            }
            
            assertableQueue = monitor.EndCapture(contextQueueId);

#endregion

#region Assert

            caughtException
                .Should().Be(null, $"Node should be started successfully");

            Context
                .GetService<IResponsibilities>()
                .SystemId
                .Should().Be(TestingNodeId, $"Since Responsibilities are configured for {TestingNodeId}");

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
                .UntilItSatisfies(x => x.Event.Equals(EventProcessor.Starting) 
                    && x.EntitySubject.Equals(EventProcessor.EventProcessorEntity), $"Event Processor should start and log an Activity");


            monitor.RemoveContextQueue(contextQueueId);
#endregion
        }


        [Fact]
        [Order(4)]
        public async Task VerifyWaitForCommandAsNoLeaderRecognizedInitially()
        {
#region Arrange

            Note testNote = new Note
            {
                UniqueHeader = TestingNodeId,
                Text = TestingNodeId,
            };

            AddNoteCommand testAddNoteCommand = new AddNoteCommand(testNote);

            var contextQueueId = Context.GetService<IActivityMonitor<Activity>>().Capture().All().Id;
            IAssertableQueue<Activity> assertableQueue = null;

#endregion

#region Act

            Exception caughtException = null;
            ClientHandlingResult clientHandlingResult = null;

            try
            {
                clientHandlingResult = await Context.GetService<IExternalClientCommandHandler>()
                    .HandleClientCommand(testAddNoteCommand, CancellationToken.None);

                assertableQueue = Context.GetService<IActivityMonitor<Activity>>().EndCapture(contextQueueId);
            }
            catch (Exception e)
            {
                caughtException = e;
            }

#endregion

#region Assert

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
                .UntilItSatisfies(x => x.Event.Equals(OnClientCommandReceive<AddNoteCommand>.RetryingAsLeaderNodeNotFound), $"Event Processor should start and log an Activity");

            Context.GetService<IActivityMonitor<Activity>>().RemoveContextQueue(contextQueueId);
#endregion
        }

        [Fact]
        [Order(5)]
        public async Task IsFollowerTurningToCandidateAndThenToLeader()
        {
            #region Arrange

            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            var notifiableQueue = monitor.Capture().All();

            var candidateEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange)
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Candidate))).RemoveOnceMatched();

            var termChanged = notifiableQueue
                .AttachNotifier(x => x.Is(TestStateProperties.PersistentPropertiesEntity, TestStateProperties.IncrementedCurrentTerm));

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

            IAssertableQueue<Activity> assertableQueue;

            Context.NodeContext.GetMockNode(MockNodeIdA).EnqueueNextRequestVoteResponse(rpc => new RequestVoteRPCResponse
            {
                Term = rpc.Term,
                VoteGranted = true
            }, approveImmediately: true);

            Context.NodeContext.GetMockNode(MockNodeIdB).EnqueueNextRequestVoteResponse(rpc => new RequestVoteRPCResponse
            {
                Term = rpc.Term,
                VoteGranted = true
            }, approveImmediately: true);

            Note testNote = new Note
            {
                UniqueHeader = nameof(testNote),
                Text = TestingNodeId,
            };

            AddNoteCommand testAddNoteCommand = new AddNoteCommand(testNote);

            #endregion

            #region Act

            Exception caughtException = null;
            ClientHandlingResult clientHandlingResult = null;

            CaptureProperties 
                captureAfterCandidacy = null, 
                captureAfterLeader = null, 
                captureAfterCommand = null, 
                captureBeforeCandidacy = null, 
                captureAfterSuccessfulAppendEntries = null;

            try
            {
                captureBeforeCandidacy = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get());

                var electionTimer = Context.GetService<IElectionTimer>() as TestElectionTimer;

                //This will make sure that the ElectionTimer callback invocation is approved for the Candidacy
                electionTimer.AwaitedLock.ApproveNext();

                //Wait until current state is Candidate
                candidateEstablished.Wait();

                ////Wait until Term incremented
                termChanged.Wait();

                ////Wait until TestRemoteManager receives a call
                remoteCallMade.Wait();

                captureAfterCandidacy = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get());

                ////Wait until ElectionSession receives a vote
                sessionReceiveVote.Wait();

                ////Wait until Majority has been attained
                majorityAttained.Wait();

                var heartBeatTimer = Context.GetService<IHeartbeatTimer>() as TestHeartbeatTimer;

                //This will make sure that the Heartbeat callback invocation is approved for SendAppendEntries
                heartBeatTimer.AwaitedLock.ApproveNext();

                //Wait until current state is now Leader
                leaderEstablished.Wait();

                captureAfterLeader = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get());

                var isPronouncedLeaderSelf = Context.GetService<ILeaderNodePronouncer>().IsLeaderRecognized 
                    && Context.GetService<ILeaderNodePronouncer>().RecognizedLeaderConfiguration.UniqueNodeId.Equals(TestingNodeId);

                Context.NodeContext.GetMockNode(MockNodeIdA).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                Context.NodeContext.GetMockNode(MockNodeIdB).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                captureAfterSuccessfulAppendEntries = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get());

                Context.NodeContext.GetMockNode(MockNodeIdA).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                Context.NodeContext.GetMockNode(MockNodeIdB).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                clientHandlingResult = await Context.GetService<IExternalClientCommandHandler>()
                    .HandleClientCommand(testAddNoteCommand, CancellationToken.None);

                captureAfterCommand = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get());
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            assertableQueue = monitor.EndCapture(notifiableQueue.Id);

            #endregion

            #region Assert

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
                .Should().Be(TestingNodeId, "Because tduring candidacy, the candidate votes for itself");

            captureAfterCandidacy
                .CurrentTerm
                .Should().Be(captureBeforeCandidacy.CurrentTerm + 1, "Since before candidacy, the term should be 1 lesser");

            captureAfterLeader
                .CommitIndex
                .Should().Be(0, "CommitIndex should be zero, since no logEntries yet");

            captureAfterLeader
                .CurrentTerm
                .Should().Be(captureAfterCandidacy.CurrentTerm, "Since Leader was elected in the candidacy election itself");

            Context.GetService<INotes>()
                .TryGet(testNote.UniqueHeader, out var note)
                .Should().BeTrue();

            note.Should().NotBeNull("Note must exist");

            note.Text.Should().Be(testNote.Text, "Note should match the testNote supplied");

            captureAfterCommand
                .LastApplied
                .Should().Be(captureAfterCommand.CommitIndex, "The command should be applied, and the lastApplied entry should be the same as the commitIndex");

            captureAfterCommand
                .MatchIndexes
                .Values
                .Should()
                .Match((collection) => collection.All(index => index.Equals(captureAfterCommand.LastLogIndex)),
                    $"{MockNodeIdA} and {MockNodeIdB} should have had replicated all entries up until the leader's last log index");

            captureAfterCommand
                .NextIndexes
                .Values
                .Should()
                .Match((collection) => collection.All(index => index.Equals(captureAfterCommand.LastLogIndex + 1)), 
                    $"{MockNodeIdA} and {MockNodeIdB} should have had replicated all entries up until the leader's last log index, and the nextIndex to send for each peer mock node should be one greater");

            monitor.RemoveContextQueue(notifiableQueue.Id);
            #endregion
        }

        [Fact]
        [Order(6)]
        public async Task IsLeaderHandlingReadOnlyCommands()
        {
            #region Arrange

            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            var notifiableQueue = monitor.Capture().All();

            IAssertableQueue<Activity> assertableQueue;

            var noteHeader = Context.GetService<INotes>().GetAllHeaders().First();
            Context.GetService<INotes>().TryGet(noteHeader, out var existingNote);

            GetNoteCommand testGetNodeCommand = new GetNoteCommand(noteHeader);

            #endregion

            #region Act

            Exception caughtException = null;
            ClientHandlingResult clientHandlingResult = null;
            CaptureProperties captureAfterReadOnlyCommand = null, captureBeforeReadOnlyCommand = null;
            try
            {
                captureBeforeReadOnlyCommand = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get());
                
                Context.NodeContext.GetMockNode(MockNodeIdA).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                Context.NodeContext.GetMockNode(MockNodeIdB).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                clientHandlingResult = await Context.GetService<IExternalClientCommandHandler>()
                    .HandleClientCommand(testGetNodeCommand, CancellationToken.None);

                captureAfterReadOnlyCommand = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get());
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            assertableQueue = monitor.EndCapture(notifiableQueue.Id);

            #endregion

            #region Assert

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
                .MatchIndexes[MockNodeIdA]
                .Should()
                .Be(captureAfterReadOnlyCommand.MatchIndexes[MockNodeIdA], "Since a Read-Only Command does not append Entries, the MatchIndex must remain the same as before");

            captureBeforeReadOnlyCommand
                .MatchIndexes[MockNodeIdB]
                .Should()
                .Be(captureAfterReadOnlyCommand.MatchIndexes[MockNodeIdB], "Since a Read-Only Command does not append Entries, the MatchIndex must remain the same as before");

            monitor.RemoveContextQueue(notifiableQueue.Id);
            #endregion
        }

        [Fact]
        [Order(7)]
        public async Task IsLeaderDenyingAppendEntriesAndRequestVote()
        {
            #region Arrange

            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            var notifiableQueue = monitor.Capture().All();

            IAssertableQueue<Activity> assertableQueue;

            var previousTerm = await Context.GetService<IPersistentProperties>().GetCurrentTerm() - 1;
            var lastLogIndexOfPrevTerm = await Context.GetService<IPersistentProperties>().LogEntries.GetLastIndexForTerm(previousTerm);
            var incompleteLogEntries = await Context.GetService<IPersistentProperties>().LogEntries.FetchLogEntriesBetween(0, lastLogIndexOfPrevTerm);

            #endregion

            #region Act

            Exception caughtException = null;
            Core.Raft.Canoe.Engine.Operational.Operation<IRequestVoteRPCResponse> requestVoteResponse = null;
            Core.Raft.Canoe.Engine.Operational.Operation<IAppendEntriesRPCResponse> appendEntriesResponse = null;

            try
            {
                requestVoteResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new RequestVoteRPC()
                {
                    CandidateId = MockNodeIdA,
                    LastLogIndex = lastLogIndexOfPrevTerm,
                    LastLogTerm = previousTerm,
                    Term = previousTerm
                }, CancellationToken.None);

                appendEntriesResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new AppendEntriesRPC(incompleteLogEntries)
                {
                    LeaderId = MockNodeIdA,
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

            assertableQueue = monitor.EndCapture(notifiableQueue.Id);

            #endregion

            #region Assert

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
                .UntilItSatisfies(_ => _.Is(OnExternalAppendEntriesRPCReceive.ActionName, OnExternalAppendEntriesRPCReceive.DeniedAppendEntries), "The denial should be stemmed from this event");

            monitor.RemoveContextQueue(notifiableQueue.Id);
            #endregion
        }

        [Fact]
        [Order(8)]
        public async Task IsLeaderHandlingConfigurationChange()
        {
            #region Arrange

            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            var notifiableQueue = monitor.Capture().All();

            var candidateEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange)
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Candidate))).RemoveOnceMatched();

            IAssertableQueue<Activity> assertableQueue;

            var currentConfiguration = Context.GetService<IClusterConfiguration>().CurrentConfiguration;

            var newConfiguration = currentConfiguration.Select(x => new NodeConfiguration
            {
                UniqueNodeId = x.UniqueNodeId

            }).Append(new NodeConfiguration
            {
                UniqueNodeId = NewNodeC //Adding NewNodeC

            }).Where(x=> !x.UniqueNodeId.Equals(MockNodeIdB)) //Removing MockNodeIdB
            .ToList();

            Context.NodeContext.CreateMockNode(NewNodeC);

            #endregion

            #region Act

            Exception caughtException = null;
            ConfigurationChangeResult changeResult = null;

            try
            {
                Context.NodeContext.GetMockNode(MockNodeIdA).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                Context.NodeContext.GetMockNode(MockNodeIdB).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

                // New Node may respond with Success false, as it has just started up and needs replication
                Context.NodeContext.GetMockNode(NewNodeC).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = rpc.PreviousLogIndex,
                    ConflictingEntryTermOnFailure = rpc.PreviousLogTerm,
                    Success = false
                }, approveImmediately: true);

                // Next time, it sends true
                Context.NodeContext.GetMockNode(NewNodeC).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = null,
                    ConflictingEntryTermOnFailure = null,
                    Success = true
                }, approveImmediately: true);

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

            assertableQueue = monitor.EndCapture(notifiableQueue.Id);

            caughtException
                .Should().Be(null, $" ");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(ClusterConfiguration.Entity, ClusterConfiguration.NewUpdate)
                    && _.HasMatchingParam(ClusterConfiguration.allNodeIds, param => param.ToString().ContainsThese(TestingNodeId, MockNodeIdA, MockNodeIdB, NewNodeC)),
                        $"Joint Consensus should account for the new {NewNodeC} and all other existing nodes");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(AppendEntriesManager.Entity, AppendEntriesManager.NewConfigurationManagement) 
                    && _.HasMatchingParam(AppendEntriesManager.nodesToAdd, param => param.ToString().Contains(NewNodeC) && param.ToString().DoesNotContainThese(MockNodeIdA, MockNodeIdB, TestingNodeId))
                        && _.HasMatchingParam(AppendEntriesManager.nodesToRemove, param => param.ToString().DoesNotContainThese(MockNodeIdA, MockNodeIdB, TestingNodeId, NewNodeC)), 
                        $"Joint Consensus should account for the new {NewNodeC} only, but should not remove {MockNodeIdB} or any other node");

            assertableQueue
                .Search()
                .UntilItSatisfies(_ => _.Is(LeaderVolatileProperties.Entity, LeaderVolatileProperties.DecrementedNextIndexToFirstIndexOfPriorValidTerm)
                    && _.HasMatchingParam(LeaderVolatileProperties.nodeId, param => param.ToString().ContainsThese(NewNodeC)),
                        $"Since entries are not yet replicated to the {NewNodeC}, the NextIndex must be decremented accordingly");

            assertableQueue
                .Search()
                .UntilItSatisfies(_ => _.Is(OnCatchUpOfNewlyAddedNodes.ActionName, OnCatchUpOfNewlyAddedNodes.NodesNotCaughtUpYet)
                    && _.HasMatchingParam(OnCatchUpOfNewlyAddedNodes.nodesToCheck, param => param.ToString().ContainsThese(NewNodeC)
                            && param.ToString().DoesNotContainThese(MockNodeIdA, MockNodeIdB, TestingNodeId)),
                        $"Catchup awaiting should only occur for {NewNodeC}, and not other nodes");

            assertableQueue
                .Search()
                .UntilItSatisfies(_ => _.Is(OnSendAppendEntriesRPC.ActionName, OnSendAppendEntriesRPC.SendingOnFailure),
                        $"Catchup awaiting should only occur for {NewNodeC}, and not other nodes");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnCatchUpOfNewlyAddedNodes.ActionName, OnCatchUpOfNewlyAddedNodes.NodesCaughtUp)
                    && _.HasMatchingParam(OnCatchUpOfNewlyAddedNodes.nodesToCheck, param => param.ToString().ContainsThese(NewNodeC) 
                            && param.ToString().DoesNotContainThese(MockNodeIdA, MockNodeIdB, TestingNodeId)),
                        $"Catchup awaiting should only occur for {NewNodeC}, and not other nodes");

            Context.NodeContext.GetMockNode(NewNodeC).AppendEntriesLock
                .RemoteCalls.Last().input.Entries.Last().Contents.As<IEnumerable<NodeConfiguration>>()
                    .Should().Match(configs => string.Join(' ', configs.Select(x => x.UniqueNodeId))
                        .ContainsThese(TestingNodeId, MockNodeIdA, MockNodeIdB, NewNodeC), 
                            $"All node Ids must be present in the Joint Consensus Configuration entry sent to the new Node as well ");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(ClusterConfiguration.Entity, ClusterConfiguration.NewUpdate)
                    && _.HasMatchingParam(ClusterConfiguration.allNodeIds, param => param.ToString().ContainsThese(TestingNodeId, MockNodeIdA, NewNodeC) 
                        && param.ToString().DoesNotContainThese(MockNodeIdB)),
                        $"Target Configuration should have the new {NewNodeC} and all other existing nodes except {MockNodeIdB}");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(AppendEntriesManager.Entity, AppendEntriesManager.NewConfigurationManagement)
                    && _.HasMatchingParam(AppendEntriesManager.nodesToAdd, param => param.ToString().DoesNotContainThese(MockNodeIdA, MockNodeIdB, TestingNodeId, NewNodeC))
                        && _.HasMatchingParam(AppendEntriesManager.nodesToRemove, param => param.ToString().Contains(MockNodeIdB) && param.ToString().DoesNotContainThese(MockNodeIdA, TestingNodeId, NewNodeC)),
                        $"When the Configuration Change is handled again by a downstream module, they will just remove {MockNodeIdB}. ");

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnConfigurationChangeRequestReceive.ActionName, OnConfigurationChangeRequestReceive.ConfigurationChangeSuccessful),
                        $"New Target Configuration Log Entry should be successfully changed");
            #endregion
        }

        [Fact]
        [Order(9)]
        public async Task IsLeaderChangingToFollowerForGreaterTermRPC()
        {
            #region Arrange

            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            var notifiableQueue = monitor.Capture().All();

            var termChanged = notifiableQueue
                .AttachNotifier(x => x.Is(TestStateProperties.PersistentPropertiesEntity, TestStateProperties.SetCurrentTermExternally));

            var followerEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange)
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Follower))).RemoveOnceMatched();

            IAssertableQueue<Activity> assertableQueue;

            var currentTerm = await Context.GetService<IPersistentProperties>().GetCurrentTerm();
            var lastLogIndexOfCurrentTerm = await Context.GetService<IPersistentProperties>().LogEntries.GetLastIndex();

            #endregion

            #region Act

            Exception caughtException = null;
            Core.Raft.Canoe.Engine.Operational.Operation<IRequestVoteRPCResponse> requestVoteResponse = null;

            try
            {
                requestVoteResponse = await Context.GetService<IExternalRpcHandler>().RespondTo(new RequestVoteRPC()
                {
                    CandidateId = MockNodeIdA,
                    LastLogIndex = lastLogIndexOfCurrentTerm + 1,
                    LastLogTerm = currentTerm,
                    Term = currentTerm + 1
                }, CancellationToken.None);

                termChanged.Wait();

                followerEstablished.Wait();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assert

            assertableQueue = monitor.EndCapture(notifiableQueue.Id);

            caughtException
                .Should().Be(null, $" ");

            termChanged.IsConditionMatched.Should().BeTrue();

            assertableQueue
                .Dig()
                .UntilItSatisfies(_ => _.Is(OnExternalRequestVoteRPCReceive.ActionName, OnExternalRequestVoteRPCReceive.RevertingToFollower), "Leader should become a follower, as it has encountered a greater term");

            Context.GetService<IPersistentProperties>().GetCurrentTerm().GetAwaiter().GetResult()
                .Should()
                .BeGreaterThan(currentTerm, "As Term must be set to the bigger Term (if encountered) from Request/Response");

            followerEstablished.IsConditionMatched.Should().BeTrue();
            #endregion
        }

        [Fact]
        [Order(int.MaxValue)]
        public async Task TestTemplateMethod2()
        {
            #region Arrange



            #endregion

            #region Act

            Exception caughtException = null;

            try
            {
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assert

            caughtException
                .Should().Be(null, $" ");

            #endregion
        }

        [Fact]
        [Order(int.MaxValue)]
        public async Task TestTemplateMethod()
        {
#region Arrange



#endregion

#region Act

            Exception caughtException = null;

            try
            {
            }
            catch (Exception e)
            {
                caughtException = e;
            }

#endregion

#region Assert

            caughtException
                .Should().Be(null, $" ");

#endregion
        }

        private Note GetTestNote(string header = null)
        {
            string testNote = nameof(testNote);

            return new Note
            {
                UniqueHeader = header ?? testNote,
                Text = TestingNodeId,
            };
        }

        public class CaptureProperties
        {
            public StateValues StateValue { get; set; }
            public long CurrentTerm { get; private set; }
            public string VotedFor { get; private set; }
            public long CommitIndex { get; set; }
            public long LastApplied { get; set; }
            public long LastLogIndex { get; set; }
            public IDictionary<string, long> MatchIndexes { get; set; } = new Dictionary<string, long>();
            public IDictionary<string, long> NextIndexes { get; set; } = new Dictionary<string, long>();

            internal CaptureProperties(IChangingState state)
            {
                StateValue = state.StateValue;

                CurrentTerm = (state as IStateDependencies).PersistentState.GetCurrentTerm().GetAwaiter().GetResult();
                VotedFor = (state as IStateDependencies).PersistentState.GetVotedFor().GetAwaiter().GetResult();

                CommitIndex = state.VolatileState.CommitIndex;
                LastApplied = state.VolatileState.LastApplied;

                LastLogIndex = (state as IStateDependencies).PersistentState.LogEntries.GetLastIndex().GetAwaiter().GetResult();

                if (state is Leader leader)
                {
                    long matchIndex, nextIndex;

                    leader.LeaderProperties.TryGetMatchIndex(MockNodeIdA, out matchIndex);
                    MatchIndexes.Add(MockNodeIdA, matchIndex);

                    leader.LeaderProperties.TryGetMatchIndex(MockNodeIdB, out matchIndex);
                    MatchIndexes.Add(MockNodeIdB, matchIndex);

                    leader.LeaderProperties.TryGetNextIndex(MockNodeIdA, out nextIndex);
                    NextIndexes.Add(MockNodeIdA, nextIndex);

                    leader.LeaderProperties.TryGetNextIndex(MockNodeIdB, out nextIndex);
                    NextIndexes.Add(MockNodeIdB, nextIndex);
                }
            }
        }
    }

    public static class ActivityMatchingExtensions
    {
        public static bool Is(this Activity e, string subject, string ev)
        {
            return e.Event.Equals(ev) && e.EntitySubject.Equals(subject);
        }

        public static bool Has(this Activity e, string paramKey, object paramValue)
        {
            return e.Parameters.Any(x=> x.Name.Equals(paramKey) && x.Value.Equals(paramValue));
        }

        public static bool HasMatchingParam(this Activity e, string paramKey, Func<object, bool> paramValuePredicate)
        {
            return e.Parameters.Any(x => x.Name.Equals(paramKey) && paramValuePredicate.Invoke(x.Value));
        }

        public static bool ContainsThese(this string paramVal, params string[] args)
        {
            bool matchesAll = true;

            foreach (var str in args)
            {
                matchesAll &= paramVal.Contains(str);
            }

            return matchesAll;
        }

        public static bool DoesNotContainThese(this string paramVal, params string[] args)
        {
            bool matchesAll = true;

            foreach (var str in args)
            {
                matchesAll &= !paramVal.Contains(str);
            }

            return matchesAll;
        }
    }

    
}
