using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Raft.Canoe.Engine.States.LeaderState
{
    internal class AppendEntriesSessionProperties
    {
        #region Constants

        private const string AppendEntriesSessionEntity = nameof(AppendEntriesSession);
        private const string NodeId = nameof(NodeId);
        private const string ReceivedResponseAlreadyExists = nameof(ReceivedResponseAlreadyExists);
        private const string SessionId = nameof(SessionId);
        private const string IsSuccessful = nameof(IsSuccessful);
        private const string ReceivedResponse = nameof(ReceivedResponse);
        private const string MajorityAttainedForSuccessfulResponses = nameof(MajorityAttainedForSuccessfulResponses);
        private const string MajorityAttainedViaLeaderConfirmation = nameof(MajorityAttainedViaLeaderConfirmation);


        #endregion

        public IActivityLogger ActivityLogger { get; }
        public IClusterConfiguration ClusterConfiguration { get; }
        public IGlobalAwaiter GlobalAwaiter { get; }




        internal int TotalServersInCluster => ClusterConfiguration.Peers.Count + 1;

        public AppendEntriesSession Session { get; init; }
        object obj = new object();
        Dictionary<string, bool> ReceivedResponses;
        bool HasFiredForMajority = false;
        bool HasFiredForLeader = false;

        public AppendEntriesSessionProperties(IActivityLogger activityLogger, IClusterConfiguration clusterConfiguration, IGlobalAwaiter globalAwaiter)
        {
            ReceivedResponses = new Dictionary<string, bool>();
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            GlobalAwaiter = globalAwaiter;
        }

        private bool IsMajorityAttainedForSuccessfulResponses()
        {
            int selfVoteCount = 1;

            return ((ReceivedResponses.Where(x => x.Value == true).Count()) + selfVoteCount) >= Math.Floor(TotalServersInCluster / 2d) + 1;
        }

        private bool IsMajorityAttainedForLeaderConfirmation()
        {
            int selfVoteCount = 1;

            return (ReceivedResponses.Count + selfVoteCount) >= Math.Floor(TotalServersInCluster / 2d) + 1;
        }

        public void ReceiveResponseFrom(string externalServerId, bool successful = true)
        {
            lock (obj)
            {
                if (ReceivedResponses.ContainsKey(externalServerId))
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Response from {externalServerId} towards session {Session.UniqueSessionId} already exists, but updating.",
                        EntitySubject = AppendEntriesSessionEntity,
                        Event = ReceivedResponseAlreadyExists,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(SessionId, Session.UniqueSessionId))
                    .With(ActivityParam.New(IsSuccessful, successful))
                    .WithCallerInfo());

                    ReceivedResponses[externalServerId] = successful;
                }
                else
                {
                    ReceivedResponses.Add(externalServerId, successful);

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Received {nameof(successful)}:{successful} Response from {externalServerId} towards session {Session.UniqueSessionId}",
                        EntitySubject = AppendEntriesSessionEntity,
                        Event = ReceivedResponse,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(SessionId, Session.UniqueSessionId))
                    .With(ActivityParam.New(IsSuccessful, successful))
                    .WithCallerInfo());
                }

                if (!HasFiredForMajority && IsMajorityAttainedForSuccessfulResponses())
                {
                    HasFiredForMajority = true;

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Majority attained for successful Responses towards session {Session.UniqueSessionId}. ${nameof(IGlobalAwaiter.SuccessfulAppendEntries)} firing",
                        EntitySubject = AppendEntriesSessionEntity,
                        Event = MajorityAttainedForSuccessfulResponses,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(SessionId, Session.UniqueSessionId))
                    .WithCallerInfo());

                    GlobalAwaiter.SuccessfulAppendEntries.SignalDone();

                }

                if (!HasFiredForLeader && IsMajorityAttainedForLeaderConfirmation())
                {
                    HasFiredForLeader = true;

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"Majority attained for successful Responses towards session {Session.UniqueSessionId}. ${nameof(IGlobalAwaiter.OngoingLeaderConfirmation)} firing",
                        EntitySubject = AppendEntriesSessionEntity,
                        Event = MajorityAttainedViaLeaderConfirmation,
                        Level = ActivityLogLevel.Debug,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(SessionId, Session.UniqueSessionId))
                    .WithCallerInfo());

                    GlobalAwaiter.OngoingLeaderConfirmation.SignalDone();
                }
            }
        }
    }
}
