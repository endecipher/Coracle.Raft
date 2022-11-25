using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    internal interface ICandidateDependencies : IStateDependencies
    {
        IElectionManager ElectionManager { get; set; }
        IRemoteManager RemoteManager { get; set; }
    }

    internal sealed class Candidate : AbstractState, ICandidateDependencies
    {
        #region Constants
        public const string Entity = nameof(Candidate);
        public const string StartingElection = nameof(StartingElection);
        public const string incrementedTerm = nameof(incrementedTerm);
        #endregion

        #region Additional Dependencies
        public IElectionManager ElectionManager { get; set; }
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
            var term = await PersistentState.IncrementCurrentTerm();

            ActivityLogger.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = StartingElection,
                Level = ActivityLogLevel.Debug
            }
            .With(ActivityParam.New(incrementedTerm, term))
            .WithCallerInfo());


            ElectionTimer.ResetWithDifferentTimeout();

            ElectionManager.Initiate(term);
        }

        public override async Task OnStateChangeBeginDisposal()
        {
            ElectionManager.CancelSessionIfExists();

            await base.OnStateChangeBeginDisposal();

            //TODO: Handle Election Timeouts in Abstract, since we don't want it to invoke any handlers until State is resumed
        }

        public override async Task OnStateEstablishment()
        {
            await base.OnStateEstablishment();

            await StartElection();
        }

        public override void HandleConfigurationChange(IEnumerable<INodeConfiguration> newPeerNodeConfigurations)
        {
            ElectionManager.HandleConfigurationChange(newPeerNodeConfigurations);
        }
    }
}
