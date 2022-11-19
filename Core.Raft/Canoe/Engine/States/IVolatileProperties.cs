namespace Core.Raft.Canoe.Engine.States
{
    internal interface IVolatileProperties
    {
        long CommitIndex { get; set; }
        long LastApplied { get; set; }
    }

    internal class VolatileProperties : IVolatileProperties
    {
        public long CommitIndex { get; set; }
        public long LastApplied { get; set; }
    }
}
