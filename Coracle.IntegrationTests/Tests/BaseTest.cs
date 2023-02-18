﻿using ActivityLogger.Logging;
using Coracle.Samples.ClientHandling.Notes;
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
using Newtonsoft.Json;
using Coracle.Raft.Engine.Logs;

namespace Coracle.IntegrationTests.Framework
{
    public abstract class BaseTest 
    {
        public const string SUT = nameof(SUT);
        public const string MockNodeA = nameof(MockNodeA);
        public const string MockNodeB = nameof(MockNodeB);
        public const string MockNewNodeC = nameof(MockNewNodeC);
        public const int Seconds = 1000;
        public const int MilliSeconds = 1;
        public const int DecidedEventProcessorQueueSize = 49;
        public const int DecidedEventProcessorWait = 100 * MilliSeconds;
        public TimeSpan EventNotificationTimeOut = TimeSpan.FromSeconds(200);

        public EngineConfigurationSettings TestEngineSettings => new EngineConfigurationSettings
        {
            NodeId = SUT,
            ThisNodeUri = null,
            DiscoveryServerUri = null,
            IncludeOriginalClientCommandInResults = true,
            WaitPostEnroll_InMilliseconds = 0,
            ProcessorQueueSize = DecidedEventProcessorQueueSize,
            ProcessorWaitTimeWhenQueueEmpty_InMilliseconds = DecidedEventProcessorWait,

            NoLeaderElectedWaitInterval_InMilliseconds = 10000 * MilliSeconds,
#if DEBUG
            ClientCommandTimeout_InMilliseconds = 1 * Seconds,
#else
            ClientCommandTimeout_InMilliseconds = 500 * MilliSeconds,
#endif
            HeartbeatInterval_InMilliseconds = 5 * Seconds,

            MinElectionTimeout_InMilliseconds = 5 * Seconds,
            MaxElectionTimeout_InMilliseconds = 6 * Seconds,

            //AppendEntries
            AppendEntriesTimeoutOnSend_InMilliseconds = 1000 * Seconds,
            AppendEntriesTimeoutOnReceive_InMilliseconds = 1000 * Seconds,

            //RequestVote
            RequestVoteTimeoutOnSend_InMilliseconds = 1000 * Seconds,
            RequestVoteTimeoutOnReceive_InMilliseconds = 1000 * Seconds,

            IncludeConfigurationChangeRequestInResults = true,
            IncludeJointConsensusConfigurationInResults = true,
            IncludeOriginalConfigurationInResults = true,

            CatchUpOfNewNodesTimeout_InMilliseconds = 100 * Seconds,
            CatchUpOfNewNodesWaitInterval_InMilliseconds = 10 * Seconds,

            CheckDepositionWaitInterval_InMilliseconds = 2 * Seconds,

            EntryCommitWaitInterval_InMilliseconds = 100 * MilliSeconds,
            EntryCommitWaitTimeout_InMilliseconds = 1000 * Seconds,

            ConfigurationChangeHandleTimeout_InMilliseconds = 1000 * Seconds,
            CompactionWaitPeriod_InMilliseconds = 2000 * Seconds,
            InstallSnapshotChunkTimeoutOnReceive_InMilliseconds = 1000 *Seconds,
            InstallSnapshotChunkTimeoutOnSend_InMilliseconds = 1000 *Seconds,
            CompactionAttemptInterval_InMilliseconds = 2 * Seconds,
            CompactionAttemptTimeout_InMilliseconds = 1000 * Seconds,
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
            Context.GetService<ICanoeNode>().InitializeConfiguration();
        }

        protected void StartNode()
        {
            Context.GetService<ICanoeNode>().Start();
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

            monitor.ClearAllQueues();
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

            var lastIndex = await Context.GetService<IPersistentProperties>().GetLastIndex();

            for (long i = 0; i <= lastIndex; i++)
            {
                var entry = await Context.GetService<IPersistentProperties>().TryGetValueAtIndex(i);

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

        internal StateCapture(IChangingState state)
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
