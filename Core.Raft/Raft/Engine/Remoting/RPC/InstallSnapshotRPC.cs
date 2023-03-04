namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IInstallSnapshotRPC : IRemoteCall
    {
        string SnapshotId { get; }
        long Term { get; }
        string LeaderId { get; }
        long LastIncludedIndex { get; }
        long LastIncludedTerm { get; }
        int Offset { get; }
        object Data { get; }
        bool Done { get; }

    }

    public class InstallSnapshotRPC : IInstallSnapshotRPC
    {
        public string SnapshotId { get; set; }
        public long Term { get; set; }
        public string LeaderId { get; set; }
        public long LastIncludedIndex { get; set; }
        public long LastIncludedTerm { get; set; }
        public int Offset { get; set; }
        public object Data { get; set; }
        public bool Done { get; set; }
    }
}