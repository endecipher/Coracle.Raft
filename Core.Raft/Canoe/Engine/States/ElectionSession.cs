using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Raft.Canoe.Engine.States
{
    /// <remarks>
    /// A candidate wins an election if it receives votes from a majority of the servers in the full cluster for the same term.
    /// Each server will vote for at most one candidate in a given term, on a first - come - first - served basis.
    /// The majority rule ensures that at most one candidate can win the election for a particular term
    /// <see cref="Section 5.2 Leader Election"/>
    /// 
    /// IMPTODO: Instead of using SessionGuid, we should use the CurrentTerm itself, as a new ElectionSession means that for the node, it is a different Term.
    /// </remarks>
    internal class ElectionSession
    {
        #region Constants

        public const string ElectionSessionEntity = nameof(ElectionSession);
        public const string Initializing = nameof(Initializing);
        public const string ReceivedVoteAlreadyExists = nameof(ReceivedVoteAlreadyExists);
        public const string SessionEnded = nameof(SessionEnded);
        public const string CancellingSession = nameof(CancellingSession);
        public const string ReceivedVote = nameof(ReceivedVote);
        public const string ReceivedVoteOfNoConfidence = nameof(ReceivedVoteOfNoConfidence);
        public const string MajorityAttained = nameof(MajorityAttained);
        public const string MajorityNotAttained = nameof(MajorityNotAttained);
        public const string NodeId = nameof(NodeId);
        public const string SessionId = nameof(SessionId);
        public const string EventKey = nameof(EventKey);

        #endregion

        IActivityLogger ActivityLogger { get; }
        IClusterConfiguration ClusterConfiguration { get; }
        IStateChanger StateChanger { get; }

        object obj = new object();

        HashSet<string> ReceivedVotesFrom;

        internal int TotalServersInCluster => ClusterConfiguration.Peers.Count + 1;

        internal Guid SessionGuid { get; private set; }

        private CancellationTokenSource VotingTokenSource { get; set; } = null;
        internal CancellationToken CancellationToken => VotingTokenSource.Token;

        public ElectionSession(IActivityLogger activityLogger, IClusterConfiguration clusterConfiguration, IStateChanger stateChanger)
        {
            ActivityLogger = activityLogger;
            ClusterConfiguration = clusterConfiguration;
            StateChanger = stateChanger;

            SessionGuid = Guid.NewGuid();

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ElectionSessionEntity,
                Event = Initializing,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(SessionId, SessionGuid))
            .WithCallerInfo());

            CancelSessionIfExists();
            ReceivedVotesFrom = new HashSet<string>();
            VotingTokenSource = new CancellationTokenSource();
        }

        private bool IsMajorityAttained()
        {
            int selfVoteCount = 1;

            return (ReceivedVotesFrom.Count + selfVoteCount) >= Math.Floor(TotalServersInCluster / 2d) + 1;
        }

        public void ReceiveVoteFrom(Guid sessionGuid, string externalServerId)
        {
            bool hasMajorityAttained = false;

            lock (obj)
            {
                if (SessionGuid.Equals(sessionGuid))
                {
                    if (ReceivedVotesFrom.Contains(externalServerId))
                    {
                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Vote from {externalServerId} towards session {sessionGuid} exists",
                            EntitySubject = ElectionSessionEntity,
                            Event = ReceivedVoteAlreadyExists,
                            Level = ActivityLogLevel.Debug,

                        }
                        .With(ActivityParam.New(NodeId, externalServerId))
                        .With(ActivityParam.New(SessionId, sessionGuid))
                        .WithCallerInfo());

                        return;
                    }
                    else
                    {
                        ActivityLogger?.Log(new CoracleActivity
                        {
                            Description = $"Received Vote from {externalServerId} towards session {sessionGuid}",
                            EntitySubject = ElectionSessionEntity,
                            Event = ReceivedVote,
                            Level = ActivityLogLevel.Debug,

                        }
                        .With(ActivityParam.New(NodeId, externalServerId))
                        .With(ActivityParam.New(SessionId, sessionGuid))
                        .WithCallerInfo());

                        ReceivedVotesFrom.Add(externalServerId);

                        /// <remarks>
                        /// 
                        /// A candidate wins an election if it receives votes from
                        /// a majority of the servers in the full cluster for the same
                        /// term.
                        /// 
                        /// 
                        /// 
                        /// Once a candidate wins an election, it becomes leader
                        /// <see cref="Section 5.2 Leader Election"/>
                        /// </remarks>

                        hasMajorityAttained = IsMajorityAttained();
                    }
                }
                else
                {
                    ActivityLogger?.Log(new CoracleActivity
                    {
                        Description = $"{sessionGuid} Request Vote has ended. Successful vote for {externalServerId} cannot be registered",
                        EntitySubject = ElectionSessionEntity,
                        Event = SessionEnded,
                        Level = ActivityLogLevel.Error,

                    }
                    .With(ActivityParam.New(NodeId, externalServerId))
                    .With(ActivityParam.New(SessionId, sessionGuid))
                    .WithCallerInfo());
                }
            }

            if (hasMajorityAttained)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Majority attained from the Vote from {externalServerId} towards session {sessionGuid}",
                    EntitySubject = ElectionSessionEntity,
                    Event = MajorityAttained,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(NodeId, externalServerId))
                .With(ActivityParam.New(SessionId, sessionGuid))
                .WithCallerInfo());

                //Lock Introduced, since multiple threads may call this method parallely, invoking StateChanger multiple times
                lock (obj)
                {
                    StateChanger.AbandonStateAndConvertTo<Leader>(nameof(Leader));
                }

                return;
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"No Majority attained from the Vote from {externalServerId} towards session {sessionGuid}",
                    EntitySubject = ElectionSessionEntity,
                    Event = MajorityNotAttained,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(NodeId, externalServerId))
                .With(ActivityParam.New(SessionId, sessionGuid))
                .WithCallerInfo());
            }
        }

        public void ReceiveNoConfidenceVoteFrom(Guid sessionGuid, string externalServerId)
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"{externalServerId} has not chosen this node during session {sessionGuid}",
                EntitySubject = ElectionSessionEntity,
                Event = ReceivedVoteOfNoConfidence,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NodeId, externalServerId))
            .With(ActivityParam.New(SessionId, sessionGuid))
            .WithCallerInfo());
        }

        internal void CancelSessionIfExists()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ElectionSessionEntity,
                Event = CancellingSession,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(SessionId, SessionGuid))
            .WithCallerInfo());

            if (VotingTokenSource != null)
            {
                VotingTokenSource.Cancel();
            }
        }
    }
}