namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IAppendEntriesRPCResponse : IRemoteResponse
    {
        long? ConflictingEntryTermOnFailure { get; }
        long? FirstIndexOfConflictingEntryTermOnFailure { get; }
        bool Success { get; }
        long Term { get; }
    }
}