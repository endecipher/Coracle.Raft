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
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Exceptions;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Configuration.Alterations;

namespace Coracle.Raft.Engine.Actions.Core
{
    internal sealed class OnConfigurationChangeRequestReceive : BaseAction<OnConfigurationChangeReceiveContext, ConfigurationChangeResult>
    {
        #region Constants

        public const string ActionName = nameof(OnConfigurationChangeRequestReceive);
        public const string commandId = nameof(commandId);
        public const string currentState = nameof(currentState);
        public const string RetryingAsLeaderNodeNotFound = nameof(RetryingAsLeaderNodeNotFound);
        public const string ForwardingCommandToLeader = nameof(ForwardingCommandToLeader);
        public const string ConfigurationChangeSuccessful = nameof(ConfigurationChangeSuccessful);

        #endregion

        private IEngineConfiguration Configuration => Input.EngineConfiguration;
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Configuration.ConfigurationChangeHandleTimeout_InMilliseconds);
        public override string UniqueName => ActionName;
        private bool IncludeConfigurationChangeRequest => Input.EngineConfiguration.IncludeConfigurationChangeRequestInResults;
        private bool IncludeJointConsensusConfiguration => Input.EngineConfiguration.IncludeJointConsensusConfigurationInResults;
        private bool IncludeOriginalConfiguration => Input.EngineConfiguration.IncludeOriginalConfigurationInResults;


        public OnConfigurationChangeRequestReceive(ConfigurationChangeRequest changeRequest, IStateDevelopment state, OnConfigurationChangeReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnConfigurationChangeReceiveContext(state, actionDependencies)
        {
            ConfigurationChange = changeRequest,
        }, activityLogger)
        { }


        #region Workflow

        protected override ConfigurationChangeResult DefaultOutput()
        {
            return new ConfigurationChangeResult
            {
                IsSuccessful = false,
                LeaderNodeConfiguration = null,
                Exception = ConfigurationChangeDeniedException.New(),
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
                .With(ActivityParam.New(commandId, Input.ConfigurationChange.UniqueRequestId))
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
            /// We would first need a special log entry, which would be able to hold the Configurations.
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
                .With(ActivityParam.New(commandId, Input.ConfigurationChange.UniqueRequestId))
                .With(ActivityParam.New(currentState, Input.State.StateValue.ToString()))
                .WithCallerInfo());

                result = DefaultOutput();
                result.LeaderNodeConfiguration = Input.LeaderNodePronouncer.RecognizedLeaderConfiguration;

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
            var jointConsensusLogEntry = await Input.PersistentState.AppendConfigurationEntry(jointConsensus);

            /// Since we are the leader, and we know we are writing a Configuration Log Entry, post successful write, we update our ClusterConfiguration
            Input.ClusterConfigurationChanger.ChangeMembership(new MembershipUpdateEvent
            {
                Configuration = jointConsensus,
                ConfigurationLogEntryIndex = jointConsensusLogEntry.CurrentIndex,
            });

            /// Alert for Replication
            /// Replicate C-old,new across Cluster
            /// 
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


            /// Wait until new nodes have become updated
            var newlyAddedNodes = jointConsensus.Where(x => x.IsNew && !x.IsOld).Select(x => x.UniqueNodeId).ToArray();

            Input.GlobalAwaiter.AwaitNodesToCatchUp(jointConsensusLogEntry.CurrentIndex, newlyAddedNodes, cancellationToken);

            /// Once nodes are caught up, it's time to append the C-new entry
            var newConfigurationLogEntry = await Input.PersistentState.AppendConfigurationEntry(Input.ConfigurationChange.NewConfiguration);

            /// Replicate C-new across Cluster
            leaderState.SendHeartbeat(null);

            /// Since we are the leader, and we know we are writing a Configuration Log Entry, post successful write, we update our ClusterConfiguration
            Input.ClusterConfigurationChanger.ChangeMembership(new MembershipUpdateEvent
            {
                Configuration = Input.ConfigurationChange.NewConfiguration,
                ConfigurationLogEntryIndex = newConfigurationLogEntry.CurrentIndex,
            }, 
            tryForReplication: true);

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ActionName,
                Event = ConfigurationChangeSuccessful,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(commandId, Input.ConfigurationChange.UniqueRequestId))
            .With(ActivityParam.New(currentState, Input.State.StateValue.ToString()))
            .WithCallerInfo());


            var output = DefaultOutput();
            output.IsSuccessful = true;
            output.LeaderNodeConfiguration = Input.LeaderNodePronouncer.RecognizedLeaderConfiguration;
            output.Exception = null;

            return output;
        }

        #endregion
    }
}
