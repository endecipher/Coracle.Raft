using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.ActivityLogger;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Raft.Engine.States.LeaderEntities
{
    /// <summary>
    /// Volatile state on leaders. 
    /// Reinitialized after election.
    /// 
    /// nextIndex[] for each server, index of the next log entry to send to that server (initialized to leader last log index + 1)
    /// matchIndex[] for each server, index of highest log entry known to be replicated on server (initialized to 0, increases monotonically)
    /// <see cref="Figure 2 State"/>
    /// </summary>
    internal class LeaderVolatileProperties : ILeaderVolatileProperties
    {
        #region Constants

        public const string indexToCheck = nameof(indexToCheck);
        public const string peerNodesWhichHaveReplicated = nameof(peerNodesWhichHaveReplicated);
        public const string totalNodesInCluster = nameof(totalNodesInCluster);
        public const string MatchIndexUpdateForMajority = nameof(MatchIndexUpdateForMajority);
        public const string Entity = nameof(LeaderVolatileProperties);
        public const string nodeId = nameof(nodeId);
        public const string newMatchIndex = nameof(newMatchIndex);
        public const string UpdatedIndices = nameof(UpdatedIndices);
        public const string NextIndex = nameof(NextIndex);
        public const string RetrieveNextIndex = nameof(RetrieveNextIndex);
        public const string currentNextIndex = nameof(currentNextIndex);
        public const string newNextIndex = nameof(newNextIndex);
        public const string DecrementedNextIndexToFirstIndexOfLeaderTermCorrespondingToConflictingIndexEntry = nameof(DecrementedNextIndexToFirstIndexOfLeaderTermCorrespondingToConflictingIndexEntry);
        public const string conflictTermOfFollower = nameof(conflictTermOfFollower);
        public const string firstIndexOfConflictingTerm = nameof(firstIndexOfConflictingTerm);
        public const string DecrementedNextIndex = nameof(DecrementedNextIndex);
        public const string DecrementedNextIndexToFirstIndexOfPriorValidTerm = nameof(DecrementedNextIndexToFirstIndexOfPriorValidTerm);
        public const string DecrementedNextIndexToFirstIndexOfConflictingTerm = nameof(DecrementedNextIndexToFirstIndexOfConflictingTerm);
        public const string Initializing = nameof(Initializing);
        public const string lastPersistedIndex = nameof(lastPersistedIndex);
        public const string NewConfigurationManagement = nameof(NewConfigurationManagement);
        public const string nodesToAdd = nameof(nodesToAdd);
        public const string nodesToRemove = nameof(nodesToRemove);

        #endregion

        IActivityLogger ActivityLogger { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        IPersistentStateHandler PersistentState { get; set; }

        public LeaderVolatileProperties(IActivityLogger activityLogger, IClusterConfiguration clusterConfiguration, IPersistentStateHandler persistentProperties)
        {
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            PersistentState = persistentProperties;
        }

        internal ConcurrentDictionary<string, ServerIndices> Indices { get; set; }

        public class ServerIndices
        {
            object _lock = new object();

            long _nextIndex;
            long _matchIndex;

            public long NextIndex
            {
                get
                {
                    return _nextIndex;
                }
                set
                {
                    lock (_lock)
                    {
                        _nextIndex = value;
                    }
                }
            }

            public long MatchIndex
            {
                get
                {
                    return _matchIndex;
                }
                set
                {
                    lock (_lock)
                    {
                        _matchIndex = value;
                    }
                }
            }
        }

        public bool TryGetNextIndex(string externalServerId, out long nextIndex)
        {
            bool isNodePresent = Indices.TryGetValue(externalServerId, out var indices);
            nextIndex = indices.NextIndex;
            return isNodePresent;
        }

        public bool TryGetMatchIndex(string externalServerId, out long matchIndex)
        {
            bool isNodePresent = Indices.TryGetValue(externalServerId, out var indices);
            matchIndex = indices.MatchIndex;
            return isNodePresent;
        }

        public void Initialize()
        {
            var lastIndex = PersistentState.GetLastIndex().GetAwaiter().GetResult();

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = Initializing,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(lastPersistedIndex, lastIndex))
            .WithCallerInfo());

            Indices = new ConcurrentDictionary<string, ServerIndices>();

            foreach (var peer in ClusterConfiguration.Peers)
            {
                string externalServerId = peer.UniqueNodeId;

                var initialValues = new ServerIndices
                {
                    MatchIndex = 0,
                    NextIndex = lastIndex + 1
                };

                Indices.AddOrUpdate(externalServerId, initialValues, (key, val) => initialValues);
            }
        }

        /// <summary>
        /// This is called when AppendEntriesRPCResponse returns a non-Success operation. 
        /// NextIndex needs to be decremented, and more logs will be sent to bring the follower up to speed.
        /// </summary>
        /// <param name="externalServerId"></param>
        public async Task DecrementNextIndex(string externalServerId, long followerConflictTerm, long followerFirstIndexOfConflictingTerm)
        {
            if (!Indices.TryGetValue(externalServerId, out var indices))
                return;

            var currentNextIndex = indices.NextIndex;

            /// <remarks>
            /// Returns the supplied term value is it is valid.
            /// If not valid, returns a previous/lesser term which is valid
            /// </remarks>
            async Task<long> GetValidTermPriorToConflictingTerm(long conflictingTermValue)
            {
                var term = conflictingTermValue;

                while (term > 0)
                {
                    bool isTermPriorAndValid = term < conflictingTermValue && await PersistentState.DoesTermExist(term);

                    if (isTermPriorAndValid)
                    {
                        break;
                    }

                    --term;
                }

                return term;
            }

            var priorValidTerm = await GetValidTermPriorToConflictingTerm(followerConflictTerm);

            /// If priorValidTerm is not the same as the supplied follower conflict term, then that means either the leader must not contain any entries from the followerConflictTerm
            /// or, the follower needs a full set of entries in any which case
            bool doesFollowerHaveInvalidTerm = !priorValidTerm.Equals(followerConflictTerm);

            var firstIndexOfPriorValidTerm = await PersistentState.GetFirstIndexForTerm(priorValidTerm);

            if (doesFollowerHaveInvalidTerm)
            {
                indices.NextIndex = firstIndexOfPriorValidTerm.Value; /// Index is present as the term is present and valid in Leader's logs 

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = DecrementedNextIndexToFirstIndexOfPriorValidTerm,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(nodeId, externalServerId))
                .With(ActivityParam.New(LeaderVolatileProperties.currentNextIndex, currentNextIndex))
                .With(ActivityParam.New(newNextIndex, firstIndexOfPriorValidTerm.Value))
                .With(ActivityParam.New(conflictTermOfFollower, followerConflictTerm))
                .With(ActivityParam.New(firstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                .WithCallerInfo());
            }
            else
            {
                var leaderConflictingIndexEntry = await PersistentState.TryGetValueAtIndex(followerFirstIndexOfConflictingTerm);

                /// If follower doesn't have an invalid term, then we must check if the logs match up until followerFirstIndexOfConflictingTerm
                if (leaderConflictingIndexEntry != null && leaderConflictingIndexEntry.Term.Equals(followerConflictTerm))
                {
                    /// Logs match up until the followerFirstIndexOfConflictingTerm, so we can send entries from
                    /// [followerFirstIndexOfConflictingTerm, leaderLastLogIndex] in the next AppendEntries RPC for the follower to confirm

                    indices.NextIndex = followerFirstIndexOfConflictingTerm;

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Log[firstIndexConflictTerm{followerFirstIndexOfConflictingTerm}].Term equals follower Conflict Term {followerConflictTerm}",
                        EntitySubject = Entity,
                        Event = DecrementedNextIndexToFirstIndexOfConflictingTerm,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(nodeId, externalServerId))
                    .With(ActivityParam.New(conflictTermOfFollower, followerConflictTerm))
                    .With(ActivityParam.New(newNextIndex, followerFirstIndexOfConflictingTerm))
                    .WithCallerInfo());
                }
                else
                {
                    /// Logs do not match up, thus we send all entries from the first index of the leader term for that index of the conflicting entry

                    var firstIndexOfleaderTermOfConflictingIndexEntry = await PersistentState.GetFirstIndexForTerm(leaderConflictingIndexEntry.Term);

                    indices.NextIndex = firstIndexOfleaderTermOfConflictingIndexEntry.Value;

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = Entity,
                        Event = DecrementedNextIndexToFirstIndexOfLeaderTermCorrespondingToConflictingIndexEntry,
                        Level = ActivityLogLevel.Debug,
                    }
                    .With(ActivityParam.New(nodeId, externalServerId))
                    .With(ActivityParam.New(LeaderVolatileProperties.currentNextIndex, currentNextIndex))
                    .With(ActivityParam.New(newNextIndex, firstIndexOfleaderTermOfConflictingIndexEntry.Value))
                    .With(ActivityParam.New(conflictTermOfFollower, followerConflictTerm))
                    .With(ActivityParam.New(firstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                    .WithCallerInfo());
                }
            }
        }

        /// <summary>
        /// This is called when AppendEntriesRPCResponse returns a non-Success operation. 
        /// NextIndex needs to be decremented, and more logs will be sent to bring the follower up to speed.
        /// </summary>
        /// <param name="externalServerId"></param>
        public async Task DecrementNextIndex(string externalServerId)
        {
            if (!Indices.TryGetValue(externalServerId, out var indices))
                return;

            var currentNextIndex = indices.NextIndex;

            var newNextIndex = await PersistentState.FetchLogEntryIndexPreviousToIndex(currentNextIndex);

            indices.NextIndex = newNextIndex;

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = DecrementedNextIndex,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(nodeId, externalServerId))
            .With(ActivityParam.New(LeaderVolatileProperties.currentNextIndex, currentNextIndex))
            .With(ActivityParam.New(LeaderVolatileProperties.newNextIndex, newNextIndex))
            .WithCallerInfo());
        }

        /// <summary>
        /// Automatically updates MatchIndex as maxIndexReplicated
        /// </summary>
        /// <param name="externalServerId"></param>
        /// <param name="maxIndexReplicated"></param>
        public void UpdateIndices(string externalServerId, long maxIndexReplicated)
        {
            if (!Indices.TryGetValue(externalServerId, out var indices))
                return;

            var nextIndexToMark = maxIndexReplicated + 1;

            indices.MatchIndex = maxIndexReplicated;
            indices.NextIndex = nextIndexToMark;

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = UpdatedIndices,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(nodeId, externalServerId))
            .With(ActivityParam.New(newMatchIndex, maxIndexReplicated))
            .With(ActivityParam.New(newNextIndex, nextIndexToMark))
            .WithCallerInfo());
        }

        public bool AreMajorityOfServersHavingEntriesUpUntilIndexReplicated(long index)
        {
            var peerNodeCountWhoHaveReplicatedGivenIndex =
                Indices.Values.Select(x => x.MatchIndex).Where(matchIndex => matchIndex >= index).Count();

            int currentNode = ClusterConfiguration.IsThisNodePartOfCluster ? 1 : default;

            var totalNodes = Indices.Count + currentNode;

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = MatchIndexUpdateForMajority,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(indexToCheck, index))
            .With(ActivityParam.New(peerNodesWhichHaveReplicated, peerNodeCountWhoHaveReplicatedGivenIndex))
            .With(ActivityParam.New(totalNodesInCluster, totalNodes))
            .WithCallerInfo());

            /// Check for Majority
            return Majority.HasAttained(peerNodeCountWhoHaveReplicatedGivenIndex + currentNode, totalNodes);
        }

        public void UpdateMembership(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {
            var newClusterMemberIds = newPeerNodeConfigurations.ToDictionary(x => x.UniqueNodeId, y => y);

            var serverIdsWhichHaveBeenRemoved = new HashSet<string>();

            foreach (var currentNodeId in Indices.Keys)
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

            var serverIdsWhichHaveBeenAdded = newClusterMemberIds;

            if (serverIdsWhichHaveBeenAdded.Count > 0)
            {
                var lastIndex = PersistentState.GetLastIndex().GetAwaiter().GetResult();

                foreach (var node in serverIdsWhichHaveBeenAdded)
                {
                    var initialValues = new ServerIndices
                    {
                        MatchIndex = 0,
                        NextIndex = lastIndex + 1
                    };

                    Indices.TryAdd(node.Key, initialValues);
                }
            }

            foreach (var nodeId in serverIdsWhichHaveBeenRemoved)
            {
                Indices.TryRemove(nodeId, out var _);
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

        public async Task<IDictionary<string, ISnapshotHeader>> RequiresSnapshot()
        {
            var existingSnapshot = await PersistentState.GetCommittedSnapshot();

            if (existingSnapshot == null) 
                return null;

            var nodesWhichRequireSnapshots = new Dictionary<string, ISnapshotHeader>();
            
            foreach (var nodeId in Indices.Keys)
            {
                if (Indices.TryGetValue(nodeId, out var indices) && indices.MatchIndex < existingSnapshot.LastIncludedIndex)
                {
                    nodesWhichRequireSnapshots.Add(nodeId, existingSnapshot);
                }
            }

            return nodesWhichRequireSnapshots;
        }
    }
}
