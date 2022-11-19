namespace Core.Raft.Canoe.Engine.States
{
    public enum StateValues
    {
        Abandoned = 0,
        Leader = 3,
        Candidate = 2,
        Follower = 1,
        NotStarted = -1
    }
}
