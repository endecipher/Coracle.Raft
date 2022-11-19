using Core.Raft.Canoe.Engine.Configuration.Cluster;

namespace Raft.Web.Canoe.Configuration
{
    public sealed class TimeConfiguration : ITimeConfiguration
    {
        public const int Seconds = 1000;

        public const int Minutes = 1000 * 60;

        public int MaxElectionTimeout_InMilliseconds { get; set; } = 10 * Minutes;

        public int MinElectionTimeout_InMilliseconds { get; set; } = 9 * Minutes;

        public int HeartbeatInterval_InMilliseconds { get; set; } = 20 * Minutes;

        public int ClientCommandRetryDelay_InMilliseconds { get; set; } = 50 * Seconds;

        public int ClientCommandTimeout_InMilliseconds { get; set; } = 10 * Minutes;

        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; set; } = 20 * Minutes;

        public int RequestVoteTimeoutOnReceive_InMilliseconds { get; set; } = 20 * Minutes;

        public int RequestVoteTimeoutOnSend_InMilliseconds { get; set; } = 20 * Minutes;

        public int AppendEntriesTimeoutOnSend_InMilliseconds { get; set; } = 20 * Minutes;
    }
}
