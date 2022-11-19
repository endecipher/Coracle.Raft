namespace Core.Raft.Canoe.Engine.States
{
    internal interface IFollowerDependencies : IStateDependencies
    {
        
    }

    internal sealed class Follower : AbstractState, IFollowerDependencies
    {
        public Follower() : base() { StateValue = StateValues.Follower; }

        #region Additional Dependencies

        #endregion

        protected override void OnElectionTimeout(object state)
        {
            StateChanger.AbandonStateAndConvertTo<Candidate>(nameof(Candidate));
        }

        /// <summary>
        /// If a follower receives no communication over a period of time called the election timeout, 
        /// then it assumes there is no viable leader and begins an election to choose a new leader.
        /// 
        /// <see cref="Section 5.2 Leader Election"/>
        /// </summary>
        public void AcknowledgeExternalRPC()
        {
            ElectionTimer.ResetWithDifferentTimeout();
        }
    }
}
