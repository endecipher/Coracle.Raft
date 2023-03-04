namespace Coracle.Raft.Engine.States
{
    public enum StateValues
    {
        Abandoned = 0,
        Stopped = 1,
        Follower = 2,
        Candidate = 4,
        Leader = 8,
    }

    public static class StateValueExtensions
    {
        public static bool IsLeader(this StateValues state)
        {
            return state == StateValues.Leader;
        }

        public static bool IsLeaderOrFollower(this StateValues state)
        {
            return state.IsLeader() || state.IsFollower();
        }

        public static bool IsCandidate(this StateValues state)
        {
            return state == StateValues.Candidate;
        }

        public static bool IsFollower(this StateValues state)
        {
            return state == StateValues.Follower;

        }

        public static bool IsStopped(this StateValues state)
        {
            return state == StateValues.Stopped;
        }

        public static bool IsAbandoned(this StateValues state)
        {
            return state == StateValues.Abandoned;
        }
    }
}
