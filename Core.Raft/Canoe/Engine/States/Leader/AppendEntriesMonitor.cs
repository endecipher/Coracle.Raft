using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using EventGuidance.DataStructures;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using System;
using System.Collections.Concurrent;

namespace Core.Raft.Canoe.Engine.States.LeaderState
{
    /// <remarks>
    /// If a follower or candidate crashes, then future RequestVote and AppendEntries RPCs sent to it will
    /// fail.Raft handles these failures by retrying indefinitely; if the crashed server restarts, then the RPC will complete
    /// successfully.If a server crashes after completing an RPC but before responding, then it will receive the same RPC
    /// again after it restarts.Raft RPCs are idempotent, so this causes no harm.For example, if a follower receives an
    /// AppendEntries request that includes log entries already present in its log, it ignores those entries in the new request
    /// <seealso cref="Section 5.5 Follower and candidate crashes"/>
    /// </remarks>
    /// 
    //TODO: Change this to a tighter outbound request sending machine. This should be the one which sends out multiple requests, not the states. 
    //Same for Election session
    //Alos, any outbound Event Action which fails, should send an event to this, so that retry can occur with fresh properties. Do not rely on times, like LastPinged times etc
    internal sealed class AppendEntriesMonitor : IDisposable
    {
        #region Constants

        private const string AppendEntriesMonitorEntity = nameof(AppendEntriesMonitor);
        private const string CreateSessionFromHeartbeat = nameof(CreateSessionFromHeartbeat);
        private const string SessionId = nameof(SessionId);
        private const string IsFromCommand = nameof(IsFromCommand);
        private const string CreateSessionFromCommand = nameof(CreateSessionFromCommand);
        private const string CommandAlreadyQueued = nameof(CommandAlreadyQueued);

        #endregion


        IActivityLogger ActivityLogger { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        IEngineConfiguration EngineConfiguration { get; }
        IGlobalAwaiter GlobalAwaiter { get; }

        internal int MaxRetryCounter => EngineConfiguration.SendAppendEntriesRPC_MaxRetryInfinityCounter;
        internal int Capacity => EngineConfiguration.SendAppendEntriesRPC_MaxSessionCapacity;
        
        /// <summary>
        /// Last Successful Timings stored for each Server
        /// </summary>
        private ConcurrentDictionary<string, DateTimeOffset> LastPingedTimes;

        /// <summary>
        /// Active AppendEntries Sessions for each Server. 
        /// If retry operations exist for a long time, while new AppendEntries Sessions are created, LRU Cache will eventually replace the old Session.
        /// </summary>
        private ConcurrentDictionary<string, RecentPropertyCache<AppendEntriesSession, AppendEntriesSessionProperties>> ActiveSessions;

        internal AppendEntriesMonitor(IActivityLogger activityLogger, IClusterConfiguration clusterConfiguration, IEngineConfiguration engineConfiguration, IGlobalAwaiter globalAwaiter)
        {
            LastPingedTimes = new ConcurrentDictionary<string, DateTimeOffset>();
            ActiveSessions = new ConcurrentDictionary<string, RecentPropertyCache<AppendEntriesSession, AppendEntriesSessionProperties>>();
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            EngineConfiguration = engineConfiguration;
            GlobalAwaiter = globalAwaiter;

            /// Initializing to the earliest Time.
            foreach (var externalServerId in ClusterConfiguration.Peers.Keys)
            {
                LastPingedTimes.TryAdd(externalServerId, DateTimeOffset.UnixEpoch);
                ActiveSessions.TryAdd(externalServerId, new RecentPropertyCache<AppendEntriesSession, AppendEntriesSessionProperties>(Capacity));
            }
        }

        internal bool CanContinue(IAppendEntriesRPCContext context)
        {
            if (context.CurrentRetryCounter >= MaxRetryCounter)
            {
                return false;
            }

            if (LastPingedTimes[context.NodeConfiguration.UniqueNodeId] > context.InvocationTime)
            {
                return false;
            }

            if (!ActiveSessions.TryGetValue(context.NodeConfiguration.UniqueNodeId, out var cache) || !cache.TryGetItem(context.Session, out var val)) 
            {
                return false;
            }

            return true;
        }

        internal void UpdateFor(AppendEntriesSession session, string peerServerId, bool success = true)
        {
            //TODO: Should UtcNow be considered? Since project can go global

            LastPingedTimes[peerServerId] = DateTimeOffset.Now;

            if (ActiveSessions.TryGetValue(peerServerId, out var cache) && cache.TryGetItem(session, out var property))
            {
                property.ReceiveResponseFrom(peerServerId, success);
            }
        }

        /// <summary>
        /// To be invoked from Heartbeat AppendEntries RPC
        /// </summary>
        /// <returns></returns>
        internal AppendEntriesSession Create()
        {
            var session = new AppendEntriesSession
            {
                UniqueSessionId = Guid.NewGuid().ToString(),
                FromCommand = false
            };

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AppendEntriesMonitorEntity,
                Event = CreateSessionFromHeartbeat,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(SessionId, session.UniqueSessionId))
            .With(ActivityParam.New(IsFromCommand, session.FromCommand))
            .WithCallerInfo());

            Add(session);

            return session;
        }

        /// <summary>
        /// To be invoked from Command Append Entries RPC
        /// </summary>
        /// <param name="commandId"></param>
        /// <returns></returns>
        internal AppendEntriesSession Create(string commandId)
        {
            var session = new AppendEntriesSession
            {
                UniqueSessionId = commandId,
                FromCommand = true
            };

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = AppendEntriesMonitorEntity,
                Event = CreateSessionFromCommand,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(SessionId, session.UniqueSessionId))
            .With(ActivityParam.New(IsFromCommand, session.FromCommand))
            .WithCallerInfo());

            Add(session);

            return session;
        }

        private void Add(AppendEntriesSession s)
        {
            foreach (var cache in ActiveSessions.Values)
            {
                if (!cache.TryAddItem(s, new AppendEntriesSessionProperties(ActivityLogger, ClusterConfiguration, GlobalAwaiter)))
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = AppendEntriesMonitorEntity,
                        Event = CommandAlreadyQueued,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(SessionId, s.UniqueSessionId))
                    .WithCallerInfo());
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
