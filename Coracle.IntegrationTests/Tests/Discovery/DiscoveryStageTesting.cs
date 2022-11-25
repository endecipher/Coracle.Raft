using ActivityLogger.Logging;
using ActivityMonitoring.Assertions.Core;
using Coracle.IntegrationTests.Components.ClientHandling.NoteCommand;
using Coracle.IntegrationTests.Components.ClientHandling.Notes;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Node;
using Coracle.IntegrationTests.Components.PersistentData;
using Coracle.IntegrationTests.Components.Remoting;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Discovery.Registrar;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Logging;
using EventGuidance.Responsibilities;
using FluentAssertions;
using System.Collections.Concurrent;
using Xunit;


namespace Coracle.IntegrationTests.Framework
{

    public class AwaitedLock
    {
        protected object _lock = new object();
        bool KeepApproving { get; set; } = false;

        public AutoResetEvent AutoResetEvent = new AutoResetEvent(false);

        //public IActionLock ContinueTesting = new ActionLock(canReset: true);

        public void ApproveNext()
        {
            lock (_lock)
            {
                KeepApproving = true;
            }

            AutoResetEvent.WaitOne();
        }

        public bool IsApproved()
        {
            lock (_lock) {

                if (KeepApproving)
                {
                    KeepApproving = false;
                    AutoResetEvent.Set();
                    return true;
                }

                return false; 
            }
        }
    }

    /// <summary>
    /// Ok, so the thing is, there will be parallel requests from TestRemoteManager for each MockNode.
    /// Once they reach that point, what we can do is, we can Wait() on a lock. Once main test thread Sets(), we reset().
    /// But, the main testing thread should be allowed to program responses. Therefore, once wait() completes, and we reset(), we call GetResponse(), passing the Input.
    /// The main testing thread can supply a list of operations in terms of funcs, and as the GetResponse is called, we can dequeue and execute.
    /// 
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public class RemoteAwaitedLock<TInput, TResponse> where TInput : class where TResponse : class
    {
        public ConcurrentQueue<(Func<TInput, object> program, ManualResetEventSlim approvalLock)> Programs { get; set; } = new ConcurrentQueue<(Func<TInput, object> program, ManualResetEventSlim approvalLock)>();

        public ManualResetEventSlim Lock = new ManualResetEventSlim();

        public ConcurrentQueue<(TInput input, object response)> RemoteCalls { get; set; } = new ConcurrentQueue<(TInput input, object response)> ();

        public ManualResetEventSlim Enqueue(Func<TInput, object> func)
        {
            var @lock = new ManualResetEventSlim(false);
            Programs.Enqueue((func, @lock));
            return @lock;
        }

        public void ApproveNextInLine()
        {
            foreach (var (program, approvalLock) in Programs)
            {
                if (approvalLock.IsSet)
                    continue;

                approvalLock.Set();
                break;
            }
        }
        
        public (TResponse response, Exception exception) WaitUntilResponse(TInput input)
        {
            if (Programs.TryDequeue(out var action))
            {
                action.approvalLock.Wait();
                action.approvalLock.Reset();

                var result = action.program.Invoke(input);

                RemoteCalls.Enqueue((input, result));

                if (result is Exception)
                {
                    return (null, (Exception) result);
                }
                else
                {
                    return ((TResponse) result, null);
                }
            }
            else
            {
                Task.Delay(4000).Wait();
            }
            throw new InvalidOperationException($"No Programs Enqueued for {typeof(TInput).AssemblyQualifiedName} and {typeof(TResponse).AssemblyQualifiedName}");
        }
    }


    //public class WaitedLocks
    //{
    //    private object _lock = new object();
    //    int ActionCounter = 0;
    //    int ApprovedCounter = 0;
    //    public IEventLock Testing = new EventLock();

    //    public void ApproveNext()
    //    {
    //        while (true)
    //        {
    //            lock (_lock)
    //            {
    //                if (LockCounters.Any())
    //                {
    //                    ApprovedCounter = LockCounters.Peek();
    //                    LockCounters.Clear();
    //                    break;
    //                }
    //            }

    //            Thread.Sleep(500);
    //        }

    //        Testing.Wait();
    //    }

    //    public Stack<int> LockCounters { get; set; } = new Stack<int>();

    //    public object RegisterNew()
    //    {
    //        lock (_lock) { LockCounters.Push(++ActionCounter); return ActionCounter; }
    //    }

    //    public bool IsApproved(object counter)
    //    {
    //        lock (_lock) { Testing.SignalDone(); return (Convert.ToInt32(counter) == ApprovedCounter);  }
    //    }
    //}

    internal class TestElectionTimer : ElectionTimer
    {
        public AwaitedLock AwaitedLock { get; set; } = new AwaitedLock();

        public TestElectionTimer(IEngineConfiguration engineConfiguration) : base(engineConfiguration)
        {

        }

        public override IElectionTimer RegisterNew(TimerCallback timerCallback)
        {
            var timeOutInMilliseconds = Convert.ToInt32(RandomTimeout.TotalMilliseconds);

            var waitedCallback = new TimerCallback((state) =>
            {
                if(AwaitedLock.IsApproved())
                    timerCallback.Invoke(state);
            });

            Timer = new Timer(waitedCallback, null, timeOutInMilliseconds, timeOutInMilliseconds);

            return this;
        }
    }

    internal class TestHeartbeatTimer : HeartbeatTimer
    {
        public AwaitedLock AwaitedLock { get; set; } = new AwaitedLock();

        public TestHeartbeatTimer(IEngineConfiguration engineConfiguration, IActivityLogger activityLogger) : base(engineConfiguration, activityLogger)
        {

        }

        public override void RegisterNew(TimerCallback timerCallback)
        {
            var waitedCallback = new TimerCallback((state) =>
            {
                if (AwaitedLock.IsApproved())
                    timerCallback.Invoke(state);
            });

            Timer = new Timer(waitedCallback, null, TimeOutInMilliseconds, TimeOutInMilliseconds);
        }
    }


    [TestCaseOrderer($"Coracle.IntegrationTests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.IntegrationTests")]
    public class DiscoveryStageTesting : IClassFixture<TestContext>
    {
        const string TestingNodeId = "SUT";
        const string MockNodeIdA = "MockA";
        const string MockNodeIdB = "MockB";
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
            HeartbeatInterval_InMilliseconds = 30 * Seconds,
            SendAppendEntriesRPC_MaxSessionCapacity = 10,

            MinElectionTimeout_InMilliseconds = 19 * Seconds,
            MaxElectionTimeout_InMilliseconds = 20 * Seconds,

            //AppendEntries
            AppendEntriesEndpoint = null,
            AppendEntriesTimeoutOnSend_InMilliseconds = 10 * Seconds,
            AppendEntriesTimeoutOnReceive_InMilliseconds = 10 * Seconds,
            SendAppendEntriesRPC_MaxRetryInfinityCounter = 20,
                
            //RequestVote
            RequestVoteEndpoint = null,
            RequestVoteTimeoutOnSend_InMilliseconds = 10 * Seconds,
            RequestVoteTimeoutOnReceive_InMilliseconds = 10 * Seconds,
            SendRequestVoteRPC_MaxRetryInfinityCounter = 20,
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
        public async Task IsTriggeringReDiscoveryWorking()
        {
#region Arrange

            Context.NodeContext.CreateMockNode(MockNodeIdA);
            Context.NodeContext.CreateMockNode(MockNodeIdB);

#endregion

#region Act

            Exception caughtException = null;

            try
            {
                await Context.GetService<INodeRegistrar>()
                    .Enroll(Context.NodeContext.GetMockNode(MockNodeIdA).Configuration, CancellationToken.None);

                await Context.GetService<INodeRegistrar>()
                    .Enroll(Context.NodeContext.GetMockNode(MockNodeIdB).Configuration, CancellationToken.None);

                await Context.GetService<IDiscoverer>().RefreshDiscovery();
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
        public void IsFollowerTurningToCandidateAndThenToLeader()
        {
            #region Arrange

            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            var notifiableQueue = monitor.Capture().All();

            var candidateEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(CurrentStateAccessor.StateHolderEntity, CurrentStateAccessor.StateChange)
                        && x.Has(CurrentStateAccessor.NewState, nameof(StateValues.Candidate))).RemoveOnceMatched();

            //var termChanged = notifiableQueue
            //    .AttachNotifier(x => x.Is(TestStateProperties.PersistentPropertiesEntity, TestStateProperties.IncrementedCurrentTerm));

            //var remoteCallMade = notifiableQueue
            //    .AttachNotifier(x => x.Is(TestRemoteManager.TestRemoteManagerEntity, TestRemoteManager.OutboundRequestVoteRPC));

            //var sessionReceiveVote = notifiableQueue
            //    .AttachNotifier(x => x.Is(ElectionSession.ElectionSessionEntity, ElectionSession.ReceivedVote));

            //var majorityAttained = notifiableQueue
            //    .AttachNotifier(x => x.Is(ElectionSession.ElectionSessionEntity, ElectionSession.MajorityAttained));

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

            #endregion

            #region Act

            Exception caughtException = null;
            CaptureProperties captureAfterBecomingLeader = null;

            try
            {
                var electionTimer = Context.GetService<IElectionTimer>() as TestElectionTimer;

                //This will make sure that the ElectionTimer callback invocation is approved for the Candidacy
                electionTimer.AwaitedLock.ApproveNext();

                //Wait until current state is Candidate
                candidateEstablished.Wait();

                ////Wait until Term incremented
                //termChanged.WaitAndRemove();

                ////Wait until TestRemoteManager receives a call
                //remoteCallMade.WaitAndRemove();

                ////Wait until ElectionSession receives a vote
                //sessionReceiveVote.WaitAndRemove();

                ////Wait until Majority has been attained
                //majorityAttained.WaitAndRemove();

                var heartBeatTimer = Context.GetService<IHeartbeatTimer>() as TestHeartbeatTimer;

                //This will make sure that the Heartbeat callback invocation is approved for SendAppendEntries
                heartBeatTimer.AwaitedLock.ApproveNext();

                //Wait until current state is now Leader
                leaderEstablished.Wait();

                captureAfterBecomingLeader = new CaptureProperties(Context.GetService<ICurrentStateAccessor>().Get() as Leader);

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
            //termChanged.IsConditionMatched.Should().BeTrue();
            //remoteCallMade.IsConditionMatched.Should().BeTrue();
            //sessionReceiveVote.IsConditionMatched.Should().BeTrue();
            //majorityAttained.IsConditionMatched.Should().BeTrue();
            leaderEstablished.IsConditionMatched.Should().BeTrue();


            captureAfterBecomingLeader
                .CommitIndex
                .Should().Be(0, "CommitIndex should be 0, since no logEntries yet");

            captureAfterBecomingLeader
                .CurrentTerm
                .Should().Be(0, "CommitIndex should be 0, since no logEntries yet");




            monitor.RemoveContextQueue(notifiableQueue.Id);
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
    }

    public class CaptureProperties
    {
        public StateValues StateValue { get; set; }
        public long CurrentTerm { get; private set; }
        public string VotedFor { get; private set; }
        public long CommitIndex { get; set; }
        public long LastApplied { get; set; }

        public IDictionary<string, long> MatchIndexes { get; set; } = new Dictionary<string, long>();
        public IDictionary<string, long> NextIndexes { get; set; } = new Dictionary<string, long>();

        internal CaptureProperties(IChangingState state)
        {
            StateValue = state.StateValue;

            CurrentTerm = (state as IStateDependencies).PersistentState.GetCurrentTerm().GetAwaiter().GetResult();
            VotedFor = (state as IStateDependencies).PersistentState.GetVotedFor().GetAwaiter().GetResult();

            CommitIndex = state.VolatileState.CommitIndex;
            LastApplied = state.VolatileState.LastApplied;

            if (state is Leader leader)
            {
                foreach (var item in leader.LeaderProperties.NextIndexForServers)
                {
                    NextIndexes.Add(item.Key, item.Value);
                }

                foreach (var item in leader.LeaderProperties.MatchIndexForServers)
                {
                    MatchIndexes.Add(item.Key, item.Value);
                }
            }
        }
    }
}
