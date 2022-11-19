using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Remoting;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    internal interface ICandidateDependencies : IStateDependencies
    {
        ElectionSession ElectionSession { get; set; }
        IRemoteManager RemoteManager { get; set; }
    }

    internal sealed class Candidate : AbstractState, ICandidateDependencies
    {
        #region Constants
        private const string CandidateEntity = nameof(Candidate);
        private const string StartingElection = nameof(StartingElection);
        #endregion

        #region Additional Dependencies
        public ElectionSession ElectionSession { get; set; }
        public IRemoteManager RemoteManager { get; set; }

        #endregion

        public Candidate() : base()
        {
            StateValue = StateValues.Candidate;
        }

        /// <remarks>
        /// The third possible outcome is that a candidate neither wins nor loses the election: if many followers become candidates at the same time, 
        /// votes could be split so that no candidate obtains a majority.When this happens, each candidate will time out and start a new election 
        /// by incrementing its term and initiating another round of RequestVote RPCs. 
        /// 
        /// However, without extra measures split votes could repeat indefinitely. 
        /// 
        /// Raft uses randomized election timeouts to ensure that split votes are rare and that they are resolved quickly. 
        /// To prevent split votes in the first place, election timeouts are chosen randomly from a fixed interval (e.g., 150–300ms).
        /// This spreads out the servers so that in most cases only a single server will time out; it wins the election and sends heartbeats before 
        /// any other servers time out. The same mechanism is used to handle split votes.
        /// Each candidate restarts its randomized election timeout at the start of an election, and it waits for that timeout to elapse before starting 
        /// the next election; this reduces the likelihood of another split vote in the new election.
        /// <see cref="Section 5.2 Leader Election"/>
        /// </remarks>
        protected override void OnElectionTimeout(object state)
        {
            StartElection().Wait();
        }

        /// <summary>
        /// On conversion to candidate, start election:
        /// • Increment currentTerm
        /// • Vote for self
        /// • Reset election timer
        /// • Send RequestVote RPCs to all other servers
        /// • If votes received from majority of servers: become leader
        /// • If AppendEntries RPC received from new leader: convert to
        /// follower
        /// • If election timeout elapses: start new election
        /// </summary>
        /// <returns></returns>
        public async Task StartElection()
        {
            ActivityLogger.Log(new CoracleActivity
            {
                EntitySubject = CandidateEntity,
                Event = StartingElection,
                Level = ActivityLogLevel.Debug
            }
            .WithCallerInfo());

            await PersistentState.IncrementCurrentTerm();

            //IMPTODO: ElectionSession should be initialized with Term and CancellationToken, if another Timeout is initiated

            ElectionTimer.ResetWithDifferentTimeout();

            var actionDependencies = new OnSendRequestVoteRPCContextDependencies
            {
                ClusterConfiguration = ClusterConfiguration,
                PersistentState = PersistentState,
                EngineConfiguration = EngineConfiguration,
                RemoteManager = RemoteManager,
                Responsibilities = Responsibilities
            };

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
            Parallel.ForEach(source: ClusterConfiguration.Peers.Values, body: (config) =>
            {
                var action = new OnSendRequestVoteRPC(input: config, ElectionSession.SessionGuid, this, actionDependencies, ActivityLogger);
                
                action.SupportCancellation();

                action.CancellationManager.Bind(ElectionSession.CancellationToken);

                Responsibilities.QueueEventAction(action, executeSeparately: false);
            },
            parallelOptions: new ParallelOptions
            {
                CancellationToken = Responsibilities.GlobalCancellationToken,
            });
        }

        public override async Task OnStateChangeBeginDisposal()
        {
            ElectionSession.CancelSessionIfExists();
            ElectionSession = null;

            await base.OnStateChangeBeginDisposal();

            //TODO: Handle Election Timeouts in Abstract, since we don't want it to invoke any handlers until State is resumed
        }

        public override async Task OnStateEstablishment()
        {
            await base.OnStateEstablishment();

            await StartElection();
        }
    }
}
