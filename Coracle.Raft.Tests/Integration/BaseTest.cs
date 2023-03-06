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
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;
using EntityMonitoring.FluentAssertions.Structure;
using Newtonsoft.Json;
using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Examples.Registrar;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Tests.Framework;
using Coracle.Raft.Engine.Command;
using Coracle.Raft.Examples.Data;
using Coracle.Raft.Tests.Components.Remoting;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Tests.Components.Helper;
using FluentAssertions;

namespace Coracle.Raft.Tests.Integration
{
    public abstract class BaseTest
    {
        public const string SUT = nameof(SUT);
        public const string SampleUri = "https://localhost";
        public const string MockNodeA = nameof(MockNodeA);
        public const string MockNodeB = nameof(MockNodeB);
        public const string MockNewNodeC = nameof(MockNewNodeC);
        public const int Seconds = 1000;
        public const int MilliSeconds = 1;
        public const int ConfiguredEventProcessorQueueSize = 100;
        public const int ConfiguredEventProcessorWait = 1 * MilliSeconds;
        public const int ConfiguredRpcEnqueueWaitInterval = 10 * MilliSeconds;
        public const int ConfiguredRpcEnqueueFailureCounter = 100;
        public TimeSpan EventNotificationTimeOut = TimeSpan.FromSeconds(10);
        public TimeSpan SmallEventNotificationTimeOut = TimeSpan.FromSeconds(2);

        public EngineConfigurationSettings TestEngineSettings => new EngineConfigurationSettings
        {
            NodeId = SUT,
            NodeUri = new Uri(SampleUri),

            IncludeOriginalClientCommandInResults = true,
            IncludeConfigurationChangeRequestInResults = true,
            IncludeJointConsensusConfigurationInResults = true,
            IncludeOriginalConfigurationInResults = true,

            WaitPostEnroll_InMilliseconds = 1 * MilliSeconds,
            HeartbeatInterval_InMilliseconds = 2 * MilliSeconds,
            NoLeaderElectedWaitInterval_InMilliseconds = 2 * MilliSeconds,
            CheckDepositionWaitInterval_InMilliseconds = 2 * MilliSeconds,
            EntryCommitWaitInterval_InMilliseconds = 2 * MilliSeconds,
            CatchUpOfNewNodesWaitInterval_InMilliseconds = 2 * MilliSeconds,

            ProcessorQueueSize = ConfiguredEventProcessorQueueSize,
            ProcessorWaitTimeWhenQueueEmpty_InMilliseconds = ConfiguredEventProcessorWait,

            ClientCommandTimeout_InMilliseconds = 2 * Seconds,

            MinElectionTimeout_InMilliseconds = 100 * MilliSeconds,
            MaxElectionTimeout_InMilliseconds = 150 * MilliSeconds,
            EntryCommitWaitTimeout_InMilliseconds = 500 * MilliSeconds,

            AppendEntriesTimeoutOnSend_InMilliseconds = 1 * Seconds,
            AppendEntriesTimeoutOnReceive_InMilliseconds = 1 * Seconds,

            RequestVoteTimeoutOnSend_InMilliseconds = 1 * Seconds,
            RequestVoteTimeoutOnReceive_InMilliseconds = 1 * Seconds,

            CatchUpOfNewNodesTimeout_InMilliseconds = 4 * Seconds,

            ConfigurationChangeHandleTimeout_InMilliseconds = 6 * Seconds,

            CompactionWaitPeriod_InMilliseconds = 1 * Seconds,
            InstallSnapshotChunkTimeoutOnReceive_InMilliseconds = 1 * Seconds,
            InstallSnapshotChunkTimeoutOnSend_InMilliseconds = 1 * Seconds,

            CompactionAttemptInterval_InMilliseconds = 10 * MilliSeconds,
            CompactionAttemptTimeout_InMilliseconds = 8 * Seconds,

            SnapshotThresholdSize = 5,
            SnapshotBufferSizeFromLastEntry = 1
        };

        protected BaseTest(TestContext context)
        {
            Context = context;
        }

        protected TestContext Context { get; }

        protected void InitializeEngineConfiguration()
        {
            (Context.GetService<IEngineConfiguration>() as EngineConfigurationSettings).ApplyFrom(TestEngineSettings);
        }

        protected void InitializeNode()
        {
            Context.GetService<ICoracleNode>().InitializeConfiguration();
        }

        protected void StartNode()
        {
            Context.GetService<ICoracleNode>().Start();
        }

        protected void CreateMockNode(string mockNodeId)
        {
            Context.NodeContext.CreateMockNode(mockNodeId);
        }

        protected void RegisterMockNodeInRegistrar(string mockNodeId)
        {
            Context.GetService<INodeRegistrar>().Enroll(new NodeConfiguration
            {
                BaseUri = null,
                UniqueNodeId = mockNodeId
            }, CancellationToken.None);
        }

        protected string GetNoteHeader(int autoGeneratedId)
        {
            return $"{nameof(Note)}{autoGeneratedId}";
        }

        protected (NoteCommand TestCommand, Note TestNote) TestAddCommand()
        {
            var nextCommandId = Context.CommandContext.NextCommandCounter;

            Note note = new Note
            {
                UniqueHeader = GetNoteHeader(nextCommandId),
            };

            note.Text = note.UniqueHeader;

            NoteCommand testCommand = NoteCommand.CreateAdd(note);

            return (TestCommand: testCommand, TestNote: note);
        }

        protected NoteCommand TestGetCommand(string header)
        {
            var command = NoteCommand.CreateGet(new Note
            {
                UniqueHeader = header
            });

            return command;
        }

        protected StateCapture CaptureStateProperties()
        {
            return new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());
        }

        protected INotifiableQueue<Activity> CaptureActivities()
        {
            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            return monitor.Capture().All();
        }

        protected IAssertableQueue<Activity> StartAssertions(INotifiableQueue<Activity> queue)
        {
            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            return monitor.EndCapture(queue.Id);
        }

        protected void Cleanup()
        {
            var monitor = Context.GetService<IActivityMonitor<Activity>>();

            monitor?.ClearAllQueues();

            foreach (var nodeId in Context.NodeContext.GetMockNodeIds())
            {
                Context.NodeContext.GetMockNode(nodeId).ClearQueues();
            }
        }

        protected void EnqueueRequestVoteSuccessResponse(string mockNodeId)
        {
            Context.NodeContext.GetMockNode(mockNodeId).EnqueueNextRequestVoteResponse(rpc => new RequestVoteRPCResponse
            {
                Term = rpc.Term,
                VoteGranted = true
            }, approveImmediately: true);
        }

        protected void EnqueueAppendEntriesSuccessResponse(string mockNodeId)
        {
            Context.NodeContext.GetMockNode(mockNodeId).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
            {
                Term = rpc.Term,
                FirstIndexOfConflictingEntryTermOnFailure = null,
                ConflictingEntryTermOnFailure = null,
                Success = true
            }, approveImmediately: true);
        }

        protected void EnqueueMultipleSuccessResponses(string mockNodeId)
        {
            for (int i = 0; i < 1000; i++)
            {
                EnqueueAppendEntriesSuccessResponse(mockNodeId);
                EnqueueRequestVoteSuccessResponse(mockNodeId);
                EnqueueInstallSnapshotSuccessResponse(mockNodeId);
            }
        }

        protected void EnqueueInstallSnapshotSuccessResponse(string mockNodeId)
        {
            Context.NodeContext.GetMockNode(mockNodeId).EnqueueNextInstallSnapshotResponse(rpc => new InstallSnapshotRPCResponse
            {
                Term = rpc.Term,
                Resend = false
            }, approveImmediately: true);
        }

        protected async Task<List<LogEntry>> GetAllLogEntries()
        {
            var list = new List<LogEntry>();

            var lastIndex = await Context.GetService<IPersistentStateHandler>().GetLastIndex();

            for (long i = 0; i <= lastIndex; i++)
            {
                var entry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtIndex(i);

                if (entry != null)
                    list.Add(entry);
            }

            return list;
        }

        public string Serialized(object data)
        {
            return JsonConvert.SerializeObject(data);
        }

        #region Turning To Leader for uncommon workflows

        protected async Task EstablishSampleLeader()
        {
            #region Arrange
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

            var candidateEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(Engine.States.Current.CurrentAcessorActivityConstants.Entity, Engine.States.Current.CurrentAcessorActivityConstants.StateChange)
                        && x.Has(Engine.States.Current.CurrentAcessorActivityConstants.newState, nameof(StateValues.Candidate))).RemoveOnceMatched();

            var termChanged = notifiableQueue
                .AttachNotifier(x => x.Is(SampleVolatileStateHandler.Entity, SampleVolatileStateHandler.IncrementedCurrentTerm));

            var remoteCallMade = notifiableQueue
                .AttachNotifier(x => x.Is(TestOutboundRequestHandler.TestRemoteManagerEntity, TestOutboundRequestHandler.OutboundRequestVoteRPC));

            var sessionReceiveVote = notifiableQueue
                .AttachNotifier(x => x.Is(ElectionManager.Entity, ElectionManager.ReceivedVote));

            var leaderEstablished = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(Engine.States.Current.CurrentAcessorActivityConstants.Entity, Engine.States.Current.CurrentAcessorActivityConstants.StateChange)
                        && x.Has(Engine.States.Current.CurrentAcessorActivityConstants.newState, nameof(StateValues.Leader))).RemoveOnceMatched();

            var updatedIndices = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(LeaderVolatileActivityConstants.Entity, LeaderVolatileActivityConstants.UpdatedIndices));


            #endregion

            #region Act

            try
            {
                InitializeEngineConfiguration();
                CreateMockNode(MockNodeA);
                CreateMockNode(MockNodeB);
                RegisterMockNodeInRegistrar(MockNodeA);
                RegisterMockNodeInRegistrar(MockNodeB);

                InitializeNode();
                StartNode();

                EnqueueMultipleSuccessResponses(MockNodeA);
                EnqueueMultipleSuccessResponses(MockNodeB);

                var electionTimer = Context.GetService<IElectionTimer>() as TestElectionTimer;

                //This will make sure that the ElectionTimer callback invocation is approved for the Candidacy
                electionTimer.AwaitedLock.ApproveNext();

                //Wait until current state is Candidate
                candidateEstablished.Wait(EventNotificationTimeOut);

                //Wait until Term incremented
                termChanged.Wait(EventNotificationTimeOut);

                //Wait until TestRemoteManager receives a call
                remoteCallMade.Wait(EventNotificationTimeOut);

                //Wait until ElectionSession receives a vote
                sessionReceiveVote.Wait(EventNotificationTimeOut);

                //Wait until Majority has been attained
                majorityAttained.Wait(EventNotificationTimeOut);

                var heartBeatTimer = Context.GetService<IHeartbeatTimer>() as TestHeartbeatTimer;

                //This will make sure that the Heartbeat callback invocation is approved for SendAppendEntries
                heartBeatTimer.AwaitedLock.ApproveNext();

                //Wait until current state is now Leader
                leaderEstablished.Wait(EventNotificationTimeOut);

                //Send parallel heartbeats for partial simulation of a real scenario
                heartBeatTimer.AwaitedLock.ApproveNext();

                var isPronouncedLeaderSelf = Context.GetService<ILeaderNodePronouncer>().IsLeaderRecognized
                    && Context.GetService<ILeaderNodePronouncer>().RecognizedLeaderConfiguration.UniqueNodeId.Equals(SUT);

                //Send parallel heartbeats for partial simulation of a real scenario
                heartBeatTimer.AwaitedLock.ApproveNext();

                updatedIndices.Wait(EventNotificationTimeOut);
                commitIndexUpdated.Wait(EventNotificationTimeOut);

                //Send parallel heartbeats for partial simulation of a real scenario
                heartBeatTimer.AwaitedLock.ApproveNext();
                updatedIndices.Wait(EventNotificationTimeOut);

                //Send parallel heartbeats for partial simulation of a real scenario
                heartBeatTimer.AwaitedLock.ApproveNext();

                clientHandlingResult = await Context.GetService<ICommandExecutor>()
                    .Execute(Command, CancellationToken.None);

                //Send parallel heartbeats for partial simulation of a real scenario
                heartBeatTimer.AwaitedLock.ApproveNext();
                updatedIndices.Wait(EventNotificationTimeOut);
                commitIndexUpdated.Wait(EventNotificationTimeOut);

                captureAfterCommand = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

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
                .Should().Be(2, "The command entry having index 2 should have been replicated to other nodes and also committed");

            captureAfterCommand
                .LastApplied
                .Should().Be(2, "The command entry having index 2 should have been replicated to other nodes and also committed");

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

        #endregion
    }

    public class StateCapture
    {
        public StateValues StateValue { get; set; }
        public long CurrentTerm { get; private set; }
        public string VotedFor { get; private set; }
        public long CommitIndex { get; set; }
        public long LastApplied { get; set; }
        public long LastLogIndex { get; set; }
        public IDictionary<string, long> MatchIndexes { get; set; } = new Dictionary<string, long>();
        public IDictionary<string, long> NextIndexes { get; set; } = new Dictionary<string, long>();

        internal StateCapture(IStateDevelopment state)
        {
            StateValue = state.StateValue;

            CurrentTerm = (state as IStateDependencies).PersistentState.GetCurrentTerm().GetAwaiter().GetResult();
            VotedFor = (state as IStateDependencies).PersistentState.GetVotedFor().GetAwaiter().GetResult();

            CommitIndex = state.VolatileState.CommitIndex;
            LastApplied = state.VolatileState.LastApplied;

            LastLogIndex = (state as IStateDependencies).PersistentState.GetLastIndex().GetAwaiter().GetResult();

            if (state is Leader leader)
            {
                foreach (var item in (leader.LeaderProperties as LeaderVolatileProperties).Indices)
                {
                    MatchIndexes.Add(item.Key, item.Value.MatchIndex);
                    NextIndexes.Add(item.Key, item.Value.NextIndex);
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
            return e.Parameters.Any(x => x.Name.Equals(paramKey) && x.Value.Equals(paramValue));
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
