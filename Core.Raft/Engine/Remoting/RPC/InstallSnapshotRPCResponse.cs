namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IInstallSnapshotRPCResponse : IRemoteResponse
    {
        long Term { get; }
        bool Resend { get; }
    }
    public class InstallSnapshotRPCResponse : IInstallSnapshotRPCResponse
    {
        public long Term { get; set; }

        public bool Resend { get; set; }
    }
}