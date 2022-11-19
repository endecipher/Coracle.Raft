using Core.Raft.Canoe.Engine.Configuration.Cluster;

namespace Raft.Web.Canoe.Configuration
{
    public sealed class OtherConfiguration : IOtherConfiguration
    {
        public int SendAppendEntriesRPC_MaxRetryInfinityCounter { get; set; } = 500;

        public int SendRequestVoteRPC_MaxRetryInfinityCounter { get; set; } = 500;

        public int SendAppendEntriesRPC_MaxSessionCapacity { get; set; } = 50;

        public bool IncludeOriginalClientCommandInResults => true;
    }
}
