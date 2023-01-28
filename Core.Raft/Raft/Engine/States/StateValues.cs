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
}
