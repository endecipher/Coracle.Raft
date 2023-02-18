using System;

namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IInstallSnapshotRPCResponse : IRemoteResponse
    {
        long Term { get; }
        bool Resend { get; }
    }

    [Serializable]
    public class InstallSnapshotRPCResponse : IInstallSnapshotRPCResponse
    {
        public long Term { get; init; }

        public bool Resend { get; init; }
    }
}