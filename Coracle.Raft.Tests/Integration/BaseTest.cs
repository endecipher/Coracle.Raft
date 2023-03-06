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
        public TimeSpan EventNotificationTimeOut = TimeSpan.FromSeconds(20);

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

            ClientCommandTimeout_InMilliseconds = 100 * MilliSeconds,

            MinElectionTimeout_InMilliseconds = 100 * MilliSeconds,
            MaxElectionTimeout_InMilliseconds = 150 * MilliSeconds,
            EntryCommitWaitTimeout_InMilliseconds = 100 * MilliSeconds,

            AppendEntriesTimeoutOnSend_InMilliseconds = 5 * Seconds,
            AppendEntriesTimeoutOnReceive_InMilliseconds = 5 * Seconds,

            RequestVoteTimeoutOnSend_InMilliseconds = 5 * Seconds,
            RequestVoteTimeoutOnReceive_InMilliseconds = 5 * Seconds,

            CatchUpOfNewNodesTimeout_InMilliseconds = 10 * Seconds,
            ConfigurationChangeHandleTimeout_InMilliseconds = 15 * Seconds,

            CompactionWaitPeriod_InMilliseconds = 1 * Seconds,
            InstallSnapshotChunkTimeoutOnReceive_InMilliseconds = 5 * Seconds,
            InstallSnapshotChunkTimeoutOnSend_InMilliseconds = 5 * Seconds,

            CompactionAttemptInterval_InMilliseconds = 10 * MilliSeconds,
            CompactionAttemptTimeout_InMilliseconds = 10 * Seconds,

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
