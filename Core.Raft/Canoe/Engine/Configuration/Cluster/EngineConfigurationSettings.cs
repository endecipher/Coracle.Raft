using System;

namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    public class EngineConfigurationSettings : IEngineConfiguration
    {
        public Uri DiscoveryServerUri { get; set; }
        public Uri ThisNodeUri { get; set; }
        public bool IncludeOriginalClientCommandInResults { get; set; }
        public string NodeId { get; set; }
        public string AppendEntriesEndpoint { get; set; }
        public string RequestVoteEndpoint { get; set; }
        public string CommandHandlingEndpoint { get; set; }
        public int SendAppendEntriesRPC_MaxRetryInfinityCounter { get; set; }
        public int SendRequestVoteRPC_MaxRetryInfinityCounter { get; set; }
        public int SendAppendEntriesRPC_MaxSessionCapacity { get; set; }
        public int WaitPostEnroll_InMilliseconds { get; set; }
        public int MaxElectionTimeout_InMilliseconds { get; set; }
        public int MinElectionTimeout_InMilliseconds { get; set; }
        public int HeartbeatInterval_InMilliseconds { get; set; }
        public int NoLeaderElectedWaitInterval_InMilliseconds { get; set; }
        public int ClientCommandTimeout_InMilliseconds { get; set; }
        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; set; }
        public int RequestVoteTimeoutOnReceive_InMilliseconds { get; set; }
        public int RequestVoteTimeoutOnSend_InMilliseconds { get; set; }
        public int AppendEntriesTimeoutOnSend_InMilliseconds { get; set; }
        public int EventProcessorQueueSize { get; set; }
        public int EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; set; }

        public void ApplyFrom(IEngineConfiguration newConfig)
        {
            DiscoveryServerUri = newConfig.DiscoveryServerUri;
            ThisNodeUri = newConfig.ThisNodeUri;
            IncludeOriginalClientCommandInResults = newConfig.IncludeOriginalClientCommandInResults;
            NodeId = newConfig.NodeId;
            AppendEntriesEndpoint = newConfig.AppendEntriesEndpoint;
            RequestVoteEndpoint = newConfig.RequestVoteEndpoint;
            CommandHandlingEndpoint = newConfig.CommandHandlingEndpoint;


            SendAppendEntriesRPC_MaxRetryInfinityCounter = newConfig.SendAppendEntriesRPC_MaxRetryInfinityCounter;
            SendRequestVoteRPC_MaxRetryInfinityCounter = newConfig.SendRequestVoteRPC_MaxRetryInfinityCounter;
            SendAppendEntriesRPC_MaxSessionCapacity = newConfig.SendAppendEntriesRPC_MaxSessionCapacity;
            WaitPostEnroll_InMilliseconds = newConfig.WaitPostEnroll_InMilliseconds;
            MaxElectionTimeout_InMilliseconds = newConfig.MaxElectionTimeout_InMilliseconds;
            MinElectionTimeout_InMilliseconds = newConfig.MinElectionTimeout_InMilliseconds;
            HeartbeatInterval_InMilliseconds = newConfig.HeartbeatInterval_InMilliseconds;
            NoLeaderElectedWaitInterval_InMilliseconds = newConfig.NoLeaderElectedWaitInterval_InMilliseconds;
            ClientCommandTimeout_InMilliseconds = newConfig.ClientCommandTimeout_InMilliseconds;
            AppendEntriesTimeoutOnReceive_InMilliseconds = newConfig.AppendEntriesTimeoutOnReceive_InMilliseconds;
            RequestVoteTimeoutOnReceive_InMilliseconds = newConfig.RequestVoteTimeoutOnReceive_InMilliseconds;
            RequestVoteTimeoutOnSend_InMilliseconds = newConfig.RequestVoteTimeoutOnSend_InMilliseconds;
            AppendEntriesTimeoutOnSend_InMilliseconds = newConfig.AppendEntriesTimeoutOnSend_InMilliseconds;
            EventProcessorQueueSize = newConfig.EventProcessorQueueSize;
            EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds = newConfig.EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds;

        }
    }
}


 
