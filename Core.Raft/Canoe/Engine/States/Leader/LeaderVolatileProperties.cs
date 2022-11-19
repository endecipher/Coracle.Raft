using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    /// <summary>
    /// Volatile state on leaders. 
    /// Reinitialized after election.
    /// 
    /// nextIndex[] for each server, index of the next log entry to send to that server (initialized to leader last log index + 1)
    /// matchIndex[] for each server, index of highest log entry known to be replicated on server (initialized to 0, increases monotonically)
    /// <see cref="Figure 2 State"/>
    /// </summary>
    internal class LeaderVolatileProperties : IDisposable
    {
        #region Constants

        private const string IndexToCheck = nameof(IndexToCheck);
        private const string TotalVotes = nameof(TotalVotes);
        private const string TotalNodes = nameof(TotalNodes);
        private const string MatchIndexUpdateForMajority = nameof(MatchIndexUpdateForMajority);
        private const string LeaderVolatilePropertiesEntity = nameof(LeaderVolatileProperties);
        private const string NodeId = nameof(NodeId);
        private const string NewMatchIndex = nameof(NewMatchIndex);
        private const string UpdatedMatchIndex = nameof(UpdatedMatchIndex);
        private const string NextIndex = nameof(NextIndex);
        private const string RetrieveNextIndex = nameof(RetrieveNextIndex);
        private const string CurrentNextIndex = nameof(CurrentNextIndex);
        private const string NewNextIndex = nameof(NewNextIndex);
        private const string UpdatedNextIndexToOne = nameof(UpdatedNextIndexToOne);
        private const string ConflictTermOfFollower = nameof(ConflictTermOfFollower);
        private const string FirstIndexOfConflictingTerm = nameof(FirstIndexOfConflictingTerm);
        private const string DecrementedNextIndex = nameof(DecrementedNextIndex);
        private const string ConflictingEntryIndexTermMatchesReceivedConflictingTerm = nameof(ConflictingEntryIndexTermMatchesReceivedConflictingTerm);
        private const string UpdatedNextIndex = nameof(UpdatedNextIndex);
        private const string Initializing = nameof(Initializing);
        private const string LastPersistedIndex = nameof(LastPersistedIndex);

        #endregion

        IActivityLogger ActivityLogger { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        IPersistentProperties PersistentState { get; set; }


        internal ConcurrentDictionary<string, long> NextIndexForServers { get; set; }
        internal ConcurrentDictionary<string, long> MatchIndexForServers { get; set; }


        public LeaderVolatileProperties(IActivityLogger activityLogger, IClusterConfiguration clusterConfiguration, IPersistentProperties persistentProperties)
        {
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            PersistentState = persistentProperties;

            NextIndexForServers = new ConcurrentDictionary<string, long>();
            MatchIndexForServers = new ConcurrentDictionary<string, long>();

            var lastIndex = PersistentState.LogEntries.GetLastIndex().GetAwaiter().GetResult();

            foreach (var peer in ClusterConfiguration.Peers)
            {
                string externalServerId = peer.Key;

                //TODO: Check if +1 works okay, else remove :/
                NextIndexForServers.TryAdd(externalServerId, lastIndex + 1);
                MatchIndexForServers.TryAdd(externalServerId, 0);
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = LeaderVolatilePropertiesEntity,
                Event = Initializing,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(LastPersistedIndex, lastIndex))
            .WithCallerInfo());
        }

        /// <summary>
        /// Automatically updates NextIndex for Server to be maxIndexReplicated + 1
        /// </summary>
        /// <param name="externalServerId"></param>
        /// <param name="maxIndexReplicated"></param>
        public void UpdateNextIndex(string externalServerId, long? maxIndexReplicated = null)
        {
            //Increases Monotonically
            if (maxIndexReplicated.HasValue && maxIndexReplicated < NextIndexForServers[externalServerId])
            {
                throw new InvalidOperationException($"NextIndex {maxIndexReplicated} cannot be lesser than already established {NextIndexForServers[externalServerId]}");
            }

            var lastIndex = maxIndexReplicated ?? PersistentState.LogEntries.GetLastIndex().GetAwaiter().GetResult();

            NextIndexForServers[externalServerId] = lastIndex + 1;

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Updated Next Index for Server {externalServerId} to {lastIndex + 1}",
                EntitySubject = LeaderVolatilePropertiesEntity,
                Event = UpdatedNextIndex,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NodeId, externalServerId))
            .With(ActivityParam.New(NewNextIndex, lastIndex + 1))
            .WithCallerInfo());
        }

        /// <summary>
        /// This is called when AppendEntriesRPCResponse returns a non-Success operation. 
        /// NextIndex needs to be decremented, and more logs will be sent to bring the follower up to speed.
        /// </summary>
        /// <param name="externalServerId"></param>
        public async Task DecrementNextIndex(string externalServerId, long? followerConflictTerm, long? followerFirstIndexOfConflictingTerm)
        {
            NextIndexForServers.TryGetValue(externalServerId, out var currentNextIndex);

            /// <remarks>
            /// If called from Heartbeat
            /// </remarks>
            if (!followerConflictTerm.HasValue || !followerFirstIndexOfConflictingTerm.HasValue)
            {
                NextIndexForServers.TryUpdate(externalServerId, currentNextIndex - 1, currentNextIndex);

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Decremented Next Index for Server {externalServerId} from {currentNextIndex} to {currentNextIndex - 1}. ConflictTerm:{followerConflictTerm} FirstIndexConflictTerm:{followerFirstIndexOfConflictingTerm}",
                    EntitySubject = LeaderVolatilePropertiesEntity,
                    Event = DecrementedNextIndex,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(NodeId, externalServerId))
                .With(ActivityParam.New(CurrentNextIndex, currentNextIndex))
                .With(ActivityParam.New(NewNextIndex, currentNextIndex - 1))
                .With(ActivityParam.New(ConflictTermOfFollower, followerConflictTerm))
                .With(ActivityParam.New(FirstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                .WithCallerInfo());

                return;
            }

            bool doesFollowerHaveInvalidTerm = false;

            while (followerConflictTerm > 0)
            {
                if (await PersistentState.LogEntries.DoesTermExist(followerConflictTerm.Value))
                {
                    break;
                }
                else
                {
                    doesFollowerHaveInvalidTerm = true;
                    followerConflictTerm--;
                }
            }

            if (doesFollowerHaveInvalidTerm)
            {
                var firstIndex = await PersistentState.LogEntries.GetFirstIndexForTerm(followerConflictTerm.Value);

                NextIndexForServers.TryUpdate(externalServerId, firstIndex.Value, currentNextIndex);

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Decremented Next Index for Server {externalServerId} from {currentNextIndex} to {firstIndex.Value}. ConflictTerm:{followerConflictTerm} FirstIndexConflictTerm:{followerFirstIndexOfConflictingTerm}. Due to response conflict term being invalid.",
                    EntitySubject = LeaderVolatilePropertiesEntity,
                    Event = DecrementedNextIndex,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(NodeId, externalServerId))
                .With(ActivityParam.New(CurrentNextIndex, currentNextIndex))
                .With(ActivityParam.New(NewNextIndex, firstIndex.Value))
                .With(ActivityParam.New(ConflictTermOfFollower, followerConflictTerm))
                .With(ActivityParam.New(FirstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                .WithCallerInfo());

                return;
            }

            /// <remarks>
            /// The below checks that if the leader's log[firstIndexOfConflictingTermFromFollower] term is the same as that of the specified term,
            /// we update to send from the 
            /// </remarks>
            if ((await PersistentState.LogEntries.GetTermAtIndex(followerFirstIndexOfConflictingTerm.Value)).Equals(followerConflictTerm))
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Log[firstIndexConflictTerm{followerFirstIndexOfConflictingTerm}].Term equals follower Conflict Term {followerConflictTerm}",
                    EntitySubject = LeaderVolatilePropertiesEntity,
                    Event = ConflictingEntryIndexTermMatchesReceivedConflictingTerm,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(NodeId, externalServerId))
                .With(ActivityParam.New(ConflictTermOfFollower, followerConflictTerm))
                .With(ActivityParam.New(FirstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                .WithCallerInfo());

                var firstIndexOfConflictTerm = await PersistentState.LogEntries.GetFirstIndexForTerm(followerConflictTerm.Value);

                if (firstIndexOfConflictTerm.Equals(followerFirstIndexOfConflictingTerm))
                {
                    NextIndexForServers.TryUpdate(externalServerId, followerFirstIndexOfConflictingTerm.Value, currentNextIndex);

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Decremented Next Index for Server {externalServerId} from {currentNextIndex} to {followerFirstIndexOfConflictingTerm.Value}. ConflictTerm:{followerConflictTerm} FirstIndexConflictTerm:{followerFirstIndexOfConflictingTerm}. Leader's First Index of Conflicting Term is equal to the follower's first Index of Conflicting Term",
                        EntitySubject = LeaderVolatilePropertiesEntity,
                        Event = DecrementedNextIndex,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(CurrentNextIndex, currentNextIndex))
                    .With(ActivityParam.New(NewNextIndex, followerFirstIndexOfConflictingTerm.Value))
                    .With(ActivityParam.New(ConflictTermOfFollower, followerConflictTerm))
                    .With(ActivityParam.New(FirstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                    .WithCallerInfo());
                }
                else if (firstIndexOfConflictTerm < followerFirstIndexOfConflictingTerm)
                {
                    NextIndexForServers.TryUpdate(externalServerId, firstIndexOfConflictTerm.Value, currentNextIndex);


                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Decremented Next Index for Server {externalServerId} from {currentNextIndex} to {firstIndexOfConflictTerm.Value}. ConflictTerm:{followerConflictTerm} FirstIndexConflictTerm:{followerFirstIndexOfConflictingTerm}. Leader's First Index of Conflicting Term is less than the follower's first Index of Conflicting Term",
                        EntitySubject = LeaderVolatilePropertiesEntity,
                        Event = DecrementedNextIndex,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(CurrentNextIndex, currentNextIndex))
                    .With(ActivityParam.New(NewNextIndex, firstIndexOfConflictTerm.Value))
                    .With(ActivityParam.New(ConflictTermOfFollower, followerConflictTerm))
                    .With(ActivityParam.New(FirstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                    .WithCallerInfo());
                }
                else if (firstIndexOfConflictTerm > followerFirstIndexOfConflictingTerm)
                {
                    var previousTerm = await PersistentState.LogEntries.GetTermAtIndex(firstIndexOfConflictTerm.Value - 1);

                    var firstIndexOfPreviousTerm = await PersistentState.LogEntries.GetFirstIndexForTerm(previousTerm);

                    NextIndexForServers.TryUpdate(externalServerId, firstIndexOfPreviousTerm.Value, currentNextIndex);


                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Decremented Next Index for Server {externalServerId} from {currentNextIndex} to {firstIndexOfPreviousTerm.Value}. ConflictTerm:{followerConflictTerm} FirstIndexConflictTerm:{followerFirstIndexOfConflictingTerm}. Leader's First Index of Conflicting Term is greater than the follower's first Index of Conflicting Term",
                        EntitySubject = LeaderVolatilePropertiesEntity,
                        Event = DecrementedNextIndex,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(CurrentNextIndex, currentNextIndex))
                    .With(ActivityParam.New(NewNextIndex, firstIndexOfPreviousTerm.Value))
                    .With(ActivityParam.New(ConflictTermOfFollower, followerConflictTerm))
                    .With(ActivityParam.New(FirstIndexOfConflictingTerm, followerFirstIndexOfConflictingTerm))
                    .WithCallerInfo());
                }

                return;
            }

            NextIndexForServers.TryUpdate(externalServerId, 1, currentNextIndex);

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Decremented Next Index for Server {externalServerId} from {currentNextIndex} to {1}. Since no condition determined.",
                EntitySubject = LeaderVolatilePropertiesEntity,
                Event = UpdatedNextIndexToOne,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NodeId, externalServerId))
            .With(ActivityParam.New(CurrentNextIndex, currentNextIndex))
            .With(ActivityParam.New(NewNextIndex, 1))
            .WithCallerInfo());
        }

        public long GetNextIndex(string externalServerId)
        {
            NextIndexForServers.TryGetValue(externalServerId, out long value);

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Fetched Next Index for server {externalServerId} : {value}",
                EntitySubject = LeaderVolatilePropertiesEntity,
                Event = RetrieveNextIndex,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NodeId, externalServerId))
            .With(ActivityParam.New(NextIndex, value))
            .WithCallerInfo());

            return value;
        }

        /// <summary>
        /// Automatically updates MatchIndex as maxIndexReplicated
        /// </summary>
        /// <param name="externalServerId"></param>
        /// <param name="maxIndexReplicated"></param>
        public void UpdateMatchIndex(string externalServerId, long? maxIndexReplicated = null)
        {
            //Increases Monotonically
            if (maxIndexReplicated.HasValue && maxIndexReplicated < MatchIndexForServers[externalServerId])
            {
                throw new InvalidOperationException($"NextIndex {maxIndexReplicated} cannot be lesser than already established {MatchIndexForServers[externalServerId]}");
            }

            var lastIndex = maxIndexReplicated ?? PersistentState.LogEntries.GetLastIndex().GetAwaiter().GetResult();

            MatchIndexForServers[externalServerId] = lastIndex;

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"Updated Match Index for Server {externalServerId} to {lastIndex}",
                EntitySubject = LeaderVolatilePropertiesEntity,
                Event = UpdatedMatchIndex,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NodeId, externalServerId))
            .With(ActivityParam.New(NewMatchIndex, lastIndex))
            .WithCallerInfo());
        }

        //TODO: Should we check if the Log Entry for the Command has been applied to a majority of the cluster INCLUSIVE of ourselves? Cuz currently only peers are being counted except the leader
        public bool HasMatchIndexUpdatedForMajority(long index)
        {
            int total = 0;

            foreach (var matchIndex in MatchIndexForServers.Values)
            {
                if (matchIndex >= index)
                {
                    ++total;
                }
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"HasMatchIndexUpdatedForMajority Check for index {index}.. Total Votes: {total} Total Servers: {MatchIndexForServers.Count}",
                EntitySubject = LeaderVolatilePropertiesEntity,
                Event = MatchIndexUpdateForMajority,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(IndexToCheck, index))
            .With(ActivityParam.New(TotalVotes, total))
            .With(ActivityParam.New(TotalNodes, MatchIndexForServers.Count))
            .WithCallerInfo());

            return total >= Math.Floor(MatchIndexForServers.Count / 2d) + 1; 
        }

        public void Dispose()
        {
            PersistentState = null;
            NextIndexForServers = null;
            MatchIndexForServers = null;
        }
    }
}
