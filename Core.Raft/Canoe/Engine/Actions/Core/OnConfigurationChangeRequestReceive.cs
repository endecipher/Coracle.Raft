using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Exceptions;
using Core.Raft.Canoe.Engine.Logs;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Structure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <summary>
    /// This is invoked from a Configuration Change RPC. Not from Log Entries.
    /// </summary>
    internal sealed class OnConfigurationChangeRequestReceive : EventAction<OnConfigurationChangeReceiveContext, ConfigurationChangeResult> 
    {
        #region Constants

        public const string ActionName = nameof(OnConfigurationChangeRequestReceive);
        public const string CommandId = nameof(CommandId);
        public const string CurrentState = nameof(CurrentState);
        public const string RetryingAsLeaderNodeNotFound = nameof(RetryingAsLeaderNodeNotFound); 
        public const string ForwardingCommandToLeader = nameof(ForwardingCommandToLeader); 
        public const string AppendingNewEntryForNonReadOnlyCommand = nameof(AppendingNewEntryForNonReadOnlyCommand); 
        public const string NotifyOtherNodes = nameof(NotifyOtherNodes); 
        public const string CommandApplied = nameof(CommandApplied); 
        public const string NodeId = nameof(NodeId); 
        public const string NodeActionName = nameof(NodeActionName); 

        #endregion

        /// <summary>
        /// The TimeConfiguration maybe changed dynamically on observation of the system
        /// </summary>
        private IEngineConfiguration Configuration => Input.EngineConfiguration;
        private IClusterConfiguration ClusterConfiguration => Input.ClusterConfiguration;
        private bool IncludeConfigurationChangeRequest => Input.EngineConfiguration.IncludeConfigurationChangeRequestInResults;
        private bool IncludeJointConsensusConfiguration => Input.EngineConfiguration.IncludeJointConsensusConfigurationInResults;
        private bool IncludeOriginalConfiguration => Input.EngineConfiguration.IncludeOriginalConfigurationInResults;

        public override string UniqueName => ActionName;

        public OnConfigurationChangeRequestReceive(ConfigurationChangeRPC changeRequest, IChangingState state, OnConfigurationChangeReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnConfigurationChangeReceiveContext(state, actionDependencies)
        {
            ConfigurationChange = changeRequest,
            InvocationTime = DateTimeOffset.UtcNow,
        }, activityLogger)
        { }

        //TODO: Proper Config
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Configuration.ClientCommandTimeout_InMilliseconds);
        protected override TimeSpan? ShouldProceedTimeOut => TimeSpan.FromMilliseconds(Configuration.ClientCommandTimeout_InMilliseconds);

        #region Workflow

        protected override ConfigurationChangeResult DefaultOutput()
        {
            return new ConfigurationChangeResult
            {
                IsOperationSuccessful = false,
                LeaderNodeConfiguration = null,
                Exception = LeaderNotFoundException.New(),
                ConfigurationChangeRequest = IncludeConfigurationChangeRequest ? Input.ConfigurationChange.NewConfiguration : null,
                JointConsensusConfiguration = 
                    IncludeJointConsensusConfiguration ? Input.ClusterConfigurationChanger
                        .CalculateJointConsensusConfigurationWith(Input.ConfigurationChange.NewConfiguration) : null,
                OriginalConfiguration = IncludeOriginalConfiguration ? Input.ClusterConfiguration.CurrentConfiguration : null,
            };
        }


        /// <summary>
        /// Clients of Raft send all of their requests to the leader.
        /// When a client first starts up, it connects to a randomly chosen server. If the client’s first choice is not the leader,
        /// that server will reject the client’s request and supply information about the most recent leader it has heard from
        /// (AppendEntries requests include the network address of
        /// the leader). If the leader crashes, client requests will time
        /// out; clients then try again with randomly-chosen servers.
        /// 
        /// <see cref="Section 8 Client Interaction"/>
        /// </summary>
        /// <returns></returns>
        protected override async Task<bool> ShouldProceed()
        {
            while (!Input.LeaderNodePronouncer.IsLeaderRecognized)
            {
                CancellationManager.ThrowIfCancellationRequested();

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Leader Node Configuration not found. Retrying..",
                    EntitySubject = ActionName,
                    Event = RetryingAsLeaderNodeNotFound,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(CommandId, Input.ConfigurationChange.UniqueId))
                .WithCallerInfo());

                await Task.Delay(Configuration.NoLeaderElectedWaitInterval_InMilliseconds, CancellationManager.CoreToken);
            }

            return await Task.FromResult(Input.IsContextValid);
        }

        protected override async Task<ConfigurationChangeResult> Action(CancellationToken cancellationToken)
        {
            ConfigurationChangeResult result = null;
            /// <remarks>
            /// Cluster configurations are stored and communicated using special entries in the replicated log; Figure 11 illustrates 
            /// the configuration change process. 
            /// 
            /// When the leader receives a request to change the configuration from C-old to C-new, it stores the configuration for joint consensus
            /// (C-old,new in the figure) as a log entry and replicates that entry using the mechanisms described previously. 
            /// 
            /// Once a given server adds the new configuration entry to its log, it uses that configuration for all future decisions (a server always 
            /// uses the latest configuration in its log, regardless of whether the entry is committed).
            /// 
            /// This means that the leader will use the rules of C-old,new to determine when the log entry for C-old,new is committed.
            /// 
            /// If the leader crashes, a new leader may be chosen under either C-old or C-old,new, depending on whether the winning candidate has received
            /// C-old,new. In any case, C-new cannot make unilateral decisions during this period.
            /// 
            /// <seealso cref="Section 6 Cluster membership changes"/>
            /// </remarks>

            /// <summary>
            /// So, in summary, we would first need a special log entry, which would be able to hold the Configurations.
            /// 
            /// "When the leader receives a request" - From here, we can understand that it is the leader's role to initiate the Joint Consensus phase.
            /// For this, we first have to make sure that the Configuration Change request is forwarded to the leader.
            /// </summary>
            if (Input.State is not Leader || !Input.IsContextValid)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Current State Value is {Input.State.StateValue}. Forwarding to Leader..",
                    EntitySubject = ActionName,
                    Event = ForwardingCommandToLeader,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(CommandId, Input.ConfigurationChange.UniqueId))
                .With(ActivityParam.New(CurrentState, Input.State.StateValue.ToString()))
                .WithCallerInfo());

                result = DefaultOutput();
                result.IsOperationSuccessful = false;
                result.LeaderNodeConfiguration = Input.LeaderNodePronouncer.RecognizedLeaderConfiguration;
                result.Exception = ConfigurationChangeDeniedException.New();

                return result;
            }


            /// <summary>
            /// When the leader receives a request to change the configuration from C-old to C-new, it stores the configuration for joint consensus
            /// (C-old,new in the figure) as a log entry and replicates that entry using the mechanisms described previously. 
            /// 
            /// <seealso cref="Section 6 Cluster membership changes"/>
            /// </summary>

            /// Since now that leader is established, we can append the configuration entry.
            var leaderState = Input.State as Leader;

            var jointConsensus = Input.ClusterConfigurationChanger.CalculateJointConsensusConfigurationWith(Input.ConfigurationChange.NewConfiguration);

            /// C-old,new log entry appended to Leader's logs
            var jointConsensusLogEntry = await Input.PersistentState.LogEntries.AppendConfigurationEntry(jointConsensus);

            /// Since we are the leader, and we know we are writing a Configuration Log Entry, post successful write, we update our ClusterConfiguration
            Input.ClusterConfigurationChanger.ApplyConfiguration(new ClusterMembershipChange
            {
                Configuration = jointConsensus,
                ConfigurationLogEntryIndex = jointConsensusLogEntry.CurrentIndex,
            });

            /// Alert for Replication
            /// 
            /// <summary>
            /// Okay, so let's think about it.
            /// 
            /// Why do we even have separate actions for Heartbeat and Logs?
            /// Okay, I get that we have a No-Op entry, i.e CommandLogEnry with HasContents = false added, but still, it's an entry at the end of the day.
            /// So, ideally speaking, We just should raise an event to the central AppendEntriesMonitor, that hey! there's some stuff which got written, can you 
            /// send out the rpc calls like normally?
            /// 
            /// And if there are calls, then we send out normally. We may send the No-Op entry to say Server 2, but send like a long list of entries to Server 3 for that same parallel outbound session,
            /// Since server 3 is not-up-to-date.
            /// 
            /// If there are any exceptions like timeout, or network exceptions for the Event Action which calls the RemoteManager to communicate with a node,
            /// then we should retry indefinitely. Therefore, we can actually make the Heartbeat shorter interval, and then we would have the retry the next time Heartbeat sends out
            /// </summary>
            /// Therefore, this call should be just raising an event to the AppendEntriesMonitor
            
            /// Replicate C-old,new across Cluster
            leaderState.SendHeartbeat(null);

            /// <remarks>
            /// Once C-old,new has been committed, neither C-old nor C-new can make decisions without approval of the other, and the
            /// Leader Completeness Property ensures that only serverswith the C-old,new log entry can be elected as leader. 
            /// 
            /// 
            /// It is now safe for the leader to create a log entry describing C-new and replicate it to the cluster.
            /// Again, this configuration will take effect on each server as soon as it is seen.
            /// 
            /// 
            /// When the new configuration has been committed under the rules of C-new, the old configuration is irrelevant and
            /// servers not in the new configuration can be shut down. As shown in Figure 11, there is no time when C-old and C-new
            /// can both make unilateral decisions; this guarantees safety.
            /// 
            /// <seealso cref="Section 6 Cluster membership changes"/>
            /// </remarks>

            /// <remarks>
            /// There are three more issues to address for reconfiguration.
            /// 
            /// 1. 
            /// The first issue is that new servers may not initially store any log entries. If they are added to the cluster in this state, 
            /// it could take quite a while for them to catch up, during which time it might not be possible to commit new log entries. 
            /// 
            /// In order to avoid availability gaps, Raft introduces an additional phase before the configuration change, in which the new servers join the cluster
            /// as non - voting members (the leader replicates log entries to them, but they are not considered for majorities).
            /// Once the new servers have caught up with the rest of the cluster, the reconfiguration can proceed as described above. 
            /// 
            /// 2.
            /// The second issue is that the cluster leader may not be part of the new configuration. 
            /// 
            /// In this case, the leader steps down (returns to follower state) once it has committed the C-new log entry. 
            /// This means that there will be a period of time (while it is committing C-new) when the leader is managing a cluster that does not include itself; 
            /// it replicates log entries but does not count itself in majorities.
            /// The leader transition occurs when C-new is committed because this is the first point when the new configuration can operate independently 
            /// (it will always be possible to choose a leader from C-new).
            /// Before this point, it may be the case that only a server from C-old can be elected leader.
            /// 
            /// 3.
            /// The third issue is that removed servers (those not in C-new) can disrupt the cluster. These servers will not receive heartbeats, so they will 
            /// time out and start new elections.They will then send RequestVote RPCs with new term numbers, and this will cause the current leader to revert to 
            /// follower state. A new leader will eventually be elected, but the removed servers will time out again and the process will repeat, resulting 
            /// in poor availability.
            /// 
            /// To prevent this problem, servers disregard RequestVote RPCs when they believe a current leader exists.
            /// Specifically, if a server receives a RequestVote RPC within the minimum election timeout of hearing from a current leader, it does not update 
            /// its term or grant its vote. 
            /// This does not affect normal elections, where each server waits at least a minimum election timeout before starting
            /// an election.
            /// However, it helps avoid disruptions from removed servers: if a leader is able to get heartbeats to its
            /// cluster, then it will not be deposed by larger term numbers.
            /// 
            /// <seealso cref="Section 6 Cluster membership changes"/>
            /// </remarks>

            /// <summary>
            /// So, let's think about it.
            /// Firstly, once we write to our Leader log the [C-old,new], i.e AppendNewConfigurationEntry, in reference to the statement 
            /// :"Once a given server adds the new configuration entry to its log, it uses that configuration for all future decisions (a server always 
            /// uses the latest configuration in its log, regardless of whether the entry is committed)."
            /// 
            /// We have to be careful, that Leader Node is precious. If the C-new doesn't have the current Leader node configuration, it would still need to wait
            /// for replicating and comitting C-new after it has done the C-old,new replication and comitting. 
            /// When other servers will iterate over the logEntries received through the normal AppendEntries RPC means, and find out that one of them 
            /// is a (HasContents = true && IsCommand = false), and they can parse the object successfully as a { IEnumerable<NodeChangeConfiguration> }, they will readily
            /// apply that Configuration to their IClusterConfiguration. That means, from then on they would start sending out votes and etc to those newly added servers also!
            /// 
            /// But here's the catch (first issue impacts) - all servers would have to treat the IsNew = true && IsOld = false NodeChangeConfigurations as non-voting members, therefore, they are not supposed to be considered for majorities. 
            /// Therefore, outbound AppendEntriesRPCs must be sent towards them for syncing of logs, however, they must be exempted 
            ///     from majority calculation that happens for updation of CommitIndex. 
            /// But, for how long would they not be considered for outbound RPCS? Well, that's the thing. Since Joint Consensus (or) Committing of C-old,new
            /// was just the first part of the ReConfiguration Phase, the second part (i.e Committing the C-new), is still yet to happen.
            /// And the Leader will not write to its log the C-new yet, as the IsNew = true && IsOld = false servers who have just been newly added do not have their 
            /// match indices updated to the CommitIndex. (RECONFIRM the correct condition for saying "up-to-date")
            /// During all of this, it is expected that the RequestVoteRPCS will not go off for any server, as the Leader's responisbility is not yet over, and the Heartbeats are supposed to keep everyone updated that 
            /// the Leader is still live and working.
            /// 
            /// Once we have the NewlyAdded servers up-to-date, the Leader will Append a new ConfigurationLogEntry with C-new.
            /// Once it writes C-new, it has to make sure to alert the Monitor again to send outbound AppendEntriesRPCs. However, it may be the case that
            /// the Leader itself is not a part of the C-new. (second-issue impacts). Therefore, the leader is precious and unique. It will have to wait until 
            /// C-new is committed, before it can decomission itself and get out of the cluster gracefully. 
            /// So, once the C-new is appended, the IClusterConfiguration change occurs to update the ClusterConfiguration used by AppendEntriesMonitor.
            /// However, where the AppendEntriesMonitor won't count itself (i.e the Leader node itself) as one of the nodes for majority. 
            /// For other servers, whenever a LogEntry is written which is ConfigurationLogEntry, the ClusterConfiguration changes are immediately applied (as Configuration Log entries
            /// were stated to not be waited for commit). However, before immediate application and change of the ClusterConfiguration, the code can contain a 
            /// check to see if the ILeaderNodePronouncer is the current node. 
            /// If it is not, then it would update the Cluster configuration. And an event is issued to decomission that server, if its nodeId is not in the configurationtoUpdate.
            /// If it is, it would check to see if it is still a part of the ClusterConfiguration.
            ///     If still a part, then no worries, and we proceed to update the ClusterConfiguration 
            ///     If not a part, then update ClusterConfiguration. But have a callback event raised which would decomission this leader server once the C-new is comitted across a majority-minus-Self servers (since we updated the ClusterConfiguration) server.
            /// 
            /// As for the thrid issue (third issue impacts), whenever a decomission event is issued, immediately the EventProcessor should be cancelled to cancel all ongoing
            /// tasks, and the StateValue could be changed to Abandoned.
            /// Ideally this should happen like when we do Pause().
            /// When new servers get added, I believe they won't have the knowledge of the ClusterConfiguration, and that C-old,new and C-new entries should actually be the one to make them update their configurations.
            /// Therefore, there should be no errors thrown if ClusterConfiguration is null/empty apart from "ThisNode". Outound RPCs will not be called. 
            /// </summary>


            /// Wait until new nodes have become updated
            var newlyAddedNodes = jointConsensus.Where(x => x.IsNew && !x.IsOld).Select(x=> x.UniqueNodeId).ToArray();

            Input.GlobalAwaiter.AwaitNodesToCatchUp(jointConsensusLogEntry.CurrentIndex, newlyAddedNodes, cancellationToken);

            /// Once nodes are caught up, it's time to append the C-new entry
            var newConfigurationLogEntry = await Input.PersistentState.LogEntries.AppendConfigurationEntry(Input.ConfigurationChange.NewConfiguration);

            /// Since we are the leader, and we know we are writing a Configuration Log Entry, post successful write, we update our ClusterConfiguration
            Input.ClusterConfigurationChanger.ApplyConfiguration(new ClusterMembershipChange
            {
                Configuration = Input.ConfigurationChange.NewConfiguration,
                ConfigurationLogEntryIndex = newConfigurationLogEntry.CurrentIndex,
            });

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Current State Value is {Input.State.StateValue}. Appending New Entry..",
                EntitySubject = ActionName,
                Event = AppendingNewEntryForNonReadOnlyCommand,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(CommandId, Input.ConfigurationChange.UniqueId))
            .With(ActivityParam.New(CurrentState, Input.State.StateValue.ToString()))
            .WithCallerInfo());


            var output = DefaultOutput();
            output.IsOperationSuccessful = true;

            return output;
        }

        #endregion
    }
}
