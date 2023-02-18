using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Logs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Coracle.Raft.Engine.Remoting.RPC
{
    public interface IInstallSnapshotRPC : IRemoteCall
    {
        string SnapshotId { get; set; }
        long Term { get; set; }
        string LeaderId { get; set; }
        long LastIncludedIndex { get; set; }
        long LastIncludedTerm { get; set; }
        int Offset { get; set; }
        object Data { get; set; }
        bool Done { get; set; }

    }

    [Serializable]
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