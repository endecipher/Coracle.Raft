namespace Rafty.Concensus.Messages
{
    public sealed class RequestVote
    {
        public RequestVote(long term, string candidateId, long lastLogIndex, long lastLogTerm)
        {
            Term = term;
            CandidateId = candidateId;
            LastLogIndex = lastLogIndex;
            LastLogTerm = lastLogTerm;
        }
        
        /// <summary>
        // Term candidate’s term.
        /// </summary>
        public long Term {get;private set;}

        /// <summary>
        // CandidateId candidate requesting vote.
        /// </summary>
        public string CandidateId {get;private set;}

        /// <summary>
        // LastLogIndex index of candidate’s last log entry (§5.4).
        /// </summary>
        public long LastLogIndex {get;private set;}

        /// <summary>
        // LastLogTerm term of candidate’s last log entry (§5.4).        
        /// </summary>
        public long LastLogTerm {get;private set;}
    }
}