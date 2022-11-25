using System;

namespace Core.Raft.Canoe.Engine.Configuration.Cluster
{
    public sealed class EngineConfigurationSettings : IEngineConfiguration
    {
        public Uri DiscoveryServerUri { get; set; }
        public int EventProcessorQueueSize { get; set; }
        public int EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; set; }
        public string NodeId { get; set; }
        public string AppendEntriesEndpoint { get; set; }
        public string RequestVoteEndpoint { get; set; }
        public string CommandHandlingEndpoint { get; set; }
        public int SendAppendEntriesRPC_MaxRetryInfinityCounter { get; set; }
        public int SendRequestVoteRPC_MaxRetryInfinityCounter { get; set; }
        public int SendAppendEntriesRPC_MaxSessionCapacity { get; set; }
        public bool IncludeOriginalClientCommandInResults { get; set; }
        public bool IncludeOriginalConfigurationInResults { get; set; }
        public bool IncludeJointConsensusConfigurationInResults { get; set; }
        public bool IncludeConfigurationChangeRequestInResults { get; set; }
        public int WaitPostEnroll_InMilliseconds { get; set; }
        public int MaxElectionTimeout_InMilliseconds { get; set; }
        public int MinElectionTimeout_InMilliseconds { get; set; }
        public int HeartbeatInterval_InMilliseconds { get; set; }
        public int NoLeaderElectedWaitInterval_InMilliseconds { get; set; }
        public int ClientCommandTimeout_InMilliseconds { get; set; }
        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; set; }
        public int CatchupIntervalOnConfigurationChange_InMilliseconds { get; set; }
        public int RequestVoteTimeoutOnReceive_InMilliseconds { get; set; }
        public int RequestVoteTimeoutOnSend_InMilliseconds { get; set; }
        public int AppendEntriesTimeoutOnSend_InMilliseconds { get; set; }
        public Uri ThisNodeUri { get; set; }
        public int EntryCommitWaitTimeout_InMilliseconds { get; set; }
        public int EntryCommitWaitInterval_InMilliseconds { get; set; }
        public int CatchUpOfNewNodesTimeout_InMilliseconds { get; set; }
        public int CatchUpOfNewNodesWaitInterval_InMilliseconds { get; set; }
        public int CheckDepositionWaitInterval_InMilliseconds { get; set; }

        public void ApplyFrom(IEngineConfiguration newConfig)
        {
            //TODO: Check all covered
            DiscoveryServerUri = newConfig.DiscoveryServerUri;
            EventProcessorQueueSize = newConfig.EventProcessorQueueSize;
            EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds = newConfig.EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds;
            NodeId = newConfig.NodeId;
            AppendEntriesEndpoint = newConfig.AppendEntriesEndpoint;
            RequestVoteEndpoint = newConfig.RequestVoteEndpoint;
            CommandHandlingEndpoint = newConfig.CommandHandlingEndpoint;
            SendAppendEntriesRPC_MaxRetryInfinityCounter = newConfig.SendAppendEntriesRPC_MaxRetryInfinityCounter;
            SendRequestVoteRPC_MaxRetryInfinityCounter = newConfig.SendRequestVoteRPC_MaxRetryInfinityCounter;
            SendAppendEntriesRPC_MaxSessionCapacity = newConfig.SendAppendEntriesRPC_MaxSessionCapacity;
            
            IncludeOriginalClientCommandInResults = newConfig.IncludeOriginalClientCommandInResults;
            IncludeOriginalConfigurationInResults = newConfig.IncludeOriginalConfigurationInResults;
            IncludeJointConsensusConfigurationInResults = newConfig.IncludeJointConsensusConfigurationInResults;
            IncludeConfigurationChangeRequestInResults = newConfig.IncludeConfigurationChangeRequestInResults;
            WaitPostEnroll_InMilliseconds = newConfig.WaitPostEnroll_InMilliseconds;
            MaxElectionTimeout_InMilliseconds = newConfig.MaxElectionTimeout_InMilliseconds;
            MinElectionTimeout_InMilliseconds = newConfig.MinElectionTimeout_InMilliseconds;
            HeartbeatInterval_InMilliseconds = newConfig.HeartbeatInterval_InMilliseconds;
            NoLeaderElectedWaitInterval_InMilliseconds = newConfig.NoLeaderElectedWaitInterval_InMilliseconds;
            ClientCommandTimeout_InMilliseconds = newConfig.ClientCommandTimeout_InMilliseconds;
            AppendEntriesTimeoutOnReceive_InMilliseconds = newConfig.AppendEntriesTimeoutOnReceive_InMilliseconds;
            CatchupIntervalOnConfigurationChange_InMilliseconds = newConfig.CatchupIntervalOnConfigurationChange_InMilliseconds;
            RequestVoteTimeoutOnReceive_InMilliseconds = newConfig.RequestVoteTimeoutOnReceive_InMilliseconds;
            RequestVoteTimeoutOnSend_InMilliseconds = newConfig.RequestVoteTimeoutOnSend_InMilliseconds;
            AppendEntriesTimeoutOnSend_InMilliseconds = newConfig.AppendEntriesTimeoutOnSend_InMilliseconds;
            ThisNodeUri = newConfig.ThisNodeUri;
            EntryCommitWaitTimeout_InMilliseconds = newConfig.EntryCommitWaitTimeout_InMilliseconds;
            EntryCommitWaitInterval_InMilliseconds = newConfig.EntryCommitWaitInterval_InMilliseconds;
            CatchUpOfNewNodesTimeout_InMilliseconds = newConfig.CatchUpOfNewNodesTimeout_InMilliseconds;
            CatchUpOfNewNodesWaitInterval_InMilliseconds = newConfig.CatchUpOfNewNodesWaitInterval_InMilliseconds;
            CheckDepositionWaitInterval_InMilliseconds = newConfig.CheckDepositionWaitInterval_InMilliseconds;

        }
    }
}


 
