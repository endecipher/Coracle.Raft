using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using EventGuidance.Responsibilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    internal sealed class ElectionManager : IElectionManager
    {
        #region Constants

        public const string Entity = nameof(ElectionManager);
        public const string Initializing = nameof(Initializing);
        public const string ReceivedVoteAlreadyExists = nameof(ReceivedVoteAlreadyExists);
        public const string CancellingSession = nameof(CancellingSession);
        public const string ReceivedVote = nameof(ReceivedVote);
        public const string ReceivedVoteOfNoConfidence = nameof(ReceivedVoteOfNoConfidence);
        public const string MajorityAttained = nameof(MajorityAttained);
        public const string MajorityNotAttained = nameof(MajorityNotAttained);
        public const string nodeId = nameof(nodeId);
        public const string electionTerm = nameof(electionTerm);
        public const string ReceivedVoteForAnotherTerm = nameof(ReceivedVoteForAnotherTerm);
        public const string receivedTerm = nameof(receivedTerm);
        public const string isVoteGranted = nameof(isVoteGranted);
        public const string ReceivedVoteFromOutOfClusterNode = nameof(ReceivedVoteFromOutOfClusterNode);
        public const string oldElectionTerm = nameof(oldElectionTerm);
        public const string NewConfigurationManagement = nameof(NewConfigurationManagement);
        public const string nodesToRemove = nameof(nodesToRemove);
        public const string nodesToAdd = nameof(nodesToAdd);

        #endregion

        object _termLock = new object();
        public long? CurrentTerm { get; private set; }
        CancellationTokenSource VotingTokenSource { get; set; }
        ConcurrentDictionary<string, NodeDetails> CurrentPeers { get; set; }

        internal class NodeDetails
        {
            public INodeConfiguration NodeConfiguration { get; init; }
            public bool VoteGranted { get; set; }
            public DateTimeOffset LastPinged { get; set; }
        }

        IActivityLogger ActivityLogger { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IRemoteManager RemoteManager { get; }
        IResponsibilities Responsibilities { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IPersistentProperties PersistentProperties { get; }

        long IElectionManager.CurrentTerm => throw new NotImplementedException();

        

        public ElectionManager(
            IActivityLogger activityLogger,
            IClusterConfiguration clusterConfiguration,
            IEngineConfiguration engineConfiguration,
            IRemoteManager remoteManager,
            IResponsibilities responsibilities,
            ICurrentStateAccessor currentStateAccessor,
            IPersistentProperties persistentProperties)
        {
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            EngineConfiguration = engineConfiguration;
            RemoteManager = remoteManager;
            Responsibilities = responsibilities;
            CurrentStateAccessor = currentStateAccessor;
            PersistentProperties = persistentProperties;

            lock (_termLock)
            {
                CurrentTerm = default;
            }
        }

        public void Initiate(long term)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = Initializing,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(oldElectionTerm, CurrentTerm))
            .With(ActivityParam.New(electionTerm, term))
            .WithCallerInfo());

            //TODO: ENSURE ONFIGURATIONS AND NEW DEPENDENCIES ARE ADDED IN REGISTRATION AND STATECHAMGER

            CancelSessionIfExists();

            VotingTokenSource = new CancellationTokenSource();

            lock (_termLock)
            {
                CurrentPeers = new ConcurrentDictionary<string, NodeDetails>();
                
                foreach (var config in ClusterConfiguration.Peers)
                {
                    CurrentPeers.TryAdd(config.UniqueNodeId, new NodeDetails 
                    { 
                        NodeConfiguration = config, 
                        LastPinged = DateTimeOffset.UnixEpoch, 
                        VoteGranted = false  
                    });
                }
                
                CurrentTerm = term;
            }

            /// <summary>
            /// Servers retry RPCs if they do not receive a response in a timely manner, and they issue RPCs in parallel for best performance.
            /// <see cref="Section 5.1 End Para"/>
            /// 
            /// To begin an election, a follower increments its current term and transitions to candidate state. 
            /// It then votes for itself and issues RequestVote RPCs in parallel to each of the other servers in the cluster.
            /// A candidate continues in this state until one of three things happens: 
            ///     (a) it wins the election, 
            ///     (b) another server establishes itself as leader, or
            ///     (c) a period of time goes by with no winner
            ///     
            /// <see cref="Section 5.2 Leader Election"/>  
            /// </summary>
            Parallel.ForEach(source: CurrentPeers.Values.Select(x=> x.NodeConfiguration), body: (config) =>
            {
                SendRequestVoteToNode(config);
            },
            parallelOptions: new ParallelOptions
            {
                CancellationToken = VotingTokenSource.Token,
            });
        }

        private void SendRequestVoteToNode(INodeConfiguration nodeConfig)
        {
            var action = new OnSendRequestVoteRPC(nodeConfig, CurrentTerm.Value, new OnSendRequestVoteRPCContextDependencies
            {
                PersistentState = PersistentProperties,
                EngineConfiguration = EngineConfiguration,
                RemoteManager = RemoteManager,
                CurrentStateAccessor = CurrentStateAccessor,

            }, ActivityLogger);

            action.SupportCancellation();

            action.CancellationManager.Bind(VotingTokenSource.Token);

            Responsibilities.QueueEventAction(action, executeSeparately: false);
        }

        private bool HasMajorityAttained(string externalServerId)
        {
            var validVotes = CurrentPeers.Values.Where(x => x.VoteGranted).Count();

            // In almost all cases, the Candidate will vote for itself first, and then start the Election.
            // However, if the Cluster Configuration changes in such a case, and the current node is no longer part of the cluster,
            // it will decomission itself, thus making no sense to go forward with the election
            if (!ClusterConfiguration.IsThisNodePartOfCluster)
            {
                CancelSessionIfExists();
            }

            const int self = 1;

            bool hasMajorityAttained = (validVotes + self) >= Math.Floor((CurrentPeers.Count + self) / 2d) + 1;

            // Lock Introduced, since multiple threads may call this method parallely, invoking StateChanger multiple times
            lock (_termLock)
            {
                /// <remarks>
                /// A candidate wins an election if it receives votes from a majority of the servers in the full cluster for the same
                /// term.
                /// 
                /// Once a candidate wins an election, it becomes leader
                /// <see cref="Section 5.2 Leader Election"/>
                /// </remarks>

                if (hasMajorityAttained)
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = Entity,
                        Event = MajorityAttained,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(nodeId, externalServerId))
                    .With(ActivityParam.New(electionTerm, CurrentTerm))
                    .WithCallerInfo());

                    var state = CurrentStateAccessor.Get();

                    if (state.StateValue.IsCandidate() && !state.IsDisposed && !VotingTokenSource.IsCancellationRequested)
                        CurrentStateAccessor.Get().StateChanger.AbandonStateAndConvertTo<Leader>(nameof(Leader));
                }

                return hasMajorityAttained;
            }
        }

        public void UpdateFor(long term, string externalServerId, bool voteGranted)
        {
            if (!CurrentPeers.TryGetValue(externalServerId, out var nodeDetails)) 
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = ReceivedVoteFromOutOfClusterNode,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodeId, externalServerId))
                .With(ActivityParam.New(electionTerm, CurrentTerm))
                .With(ActivityParam.New(receivedTerm, term))
                .With(ActivityParam.New(isVoteGranted, voteGranted))
                .WithCallerInfo());

                return;
            }

            nodeDetails.LastPinged = DateTime.UtcNow;
                //.Information.LastPinged = DateTimeOffset.UtcNow;

            if (!term.Equals(CurrentTerm))
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = ReceivedVoteForAnotherTerm,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodeId, externalServerId))
                .With(ActivityParam.New(electionTerm, CurrentTerm))
                .With(ActivityParam.New(receivedTerm, term))
                .With(ActivityParam.New(isVoteGranted, voteGranted))
                .WithCallerInfo());

                return;
            }

            if (!voteGranted)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"{externalServerId} has not chosen this node during this election",
                    EntitySubject = Entity,
                    Event = ReceivedVoteOfNoConfidence,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(nodeId, externalServerId))
                .With(ActivityParam.New(electionTerm, CurrentTerm))
                .WithCallerInfo());

                return;
            }

            bool isVoteAlreadyGranted = nodeDetails.VoteGranted;

            if (isVoteAlreadyGranted)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = ReceivedVoteAlreadyExists,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodeId, externalServerId))
                .With(ActivityParam.New(electionTerm, CurrentTerm))
                .WithCallerInfo());

                return;
            }

            nodeDetails.VoteGranted = voteGranted; // Vote Granted is true, thus we update the findings and check for Majority

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = ReceivedVote,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(nodeId, externalServerId))
            .With(ActivityParam.New(electionTerm, CurrentTerm))
            .WithCallerInfo());

            if (!HasMajorityAttained(externalServerId))
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"No Majority attained yet from the vote of {externalServerId}",
                    EntitySubject = Entity,
                    Event = MajorityNotAttained,
                    Level = ActivityLogLevel.Debug,
                }
                .With(ActivityParam.New(nodeId, externalServerId))
                .With(ActivityParam.New(electionTerm, CurrentTerm))
                .With(ActivityParam.New(isVoteGranted, voteGranted))
                .WithCallerInfo());
            }
        }

        public void CancelSessionIfExists()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = CancellingSession,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(electionTerm, CurrentTerm))
            .WithCallerInfo());

            if (VotingTokenSource != null)
            {
                VotingTokenSource.Cancel();
            }

            lock (_termLock)
            {
                CurrentPeers = new ConcurrentDictionary<string, NodeDetails>();

                CurrentTerm = default;
            }
        }

        public bool CanSendTowards(string nodeId, long actionElectionTerm)
        {
            return CurrentTerm.HasValue && CurrentTerm.Value == actionElectionTerm 
                && CurrentPeers.ContainsKey(nodeId) && !VotingTokenSource.IsCancellationRequested;
        }

        public void IssueRetry(string uniqueNodeId)
        {
            if (CurrentPeers.TryGetValue(uniqueNodeId, out var nodeDetails))
            {
                nodeDetails.LastPinged = DateTimeOffset.UtcNow;

                new Task(() =>
                {
                    SendRequestVoteToNode(nodeDetails.NodeConfiguration);

                }).Start();
            }
        }

        public void HandleConfigurationChange(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {
            var newClusterMemberIds = newPeerNodeConfigurations.ToDictionary(x => x.UniqueNodeId, y => y);

            var serverIdsWhichHaveBeenRemoved = new HashSet<string>();

            foreach (var currentNodeId in CurrentPeers.Keys)
            {
                if (newClusterMemberIds.ContainsKey(currentNodeId))
                {
                    newClusterMemberIds.Remove(currentNodeId);
                }
                else
                {
                    serverIdsWhichHaveBeenRemoved.Add(currentNodeId);
                }
            }

            // If any remaining, which have not been removed yet
            var serverIdsWhichHaveBeenAdded = newClusterMemberIds;

            // Since we want the Cluster Configuration to change immediately, any RequestVote actions which might be performing now
            // will have to check CurrentNodeIds whether they can perform or not

            foreach (var node in serverIdsWhichHaveBeenAdded)
            {
                CurrentPeers.TryAdd(node.Key, new NodeDetails 
                { 
                    NodeConfiguration = node.Value, 
                    LastPinged = DateTimeOffset.UnixEpoch, 
                    VoteGranted = false 
                });
            }

            foreach (var nodeId in serverIdsWhichHaveBeenRemoved)
            {
                CurrentPeers.TryRemove(nodeId, out var removed);
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = NewConfigurationManagement,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(nodesToRemove, serverIdsWhichHaveBeenRemoved))
            .With(ActivityParam.New(nodesToAdd, serverIdsWhichHaveBeenAdded))
            .WithCallerInfo());
        }
    }
}