﻿using System;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    public sealed class EngineConfigurationSettings : IEngineConfiguration
    {
        public Uri DiscoveryServerUri { get; set; }
        public int ProcessorQueueSize { get; set; }
        public int ProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; set; }
        public string NodeId { get; set; }
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
        public int ConfigurationChangeHandleTimeout_InMilliseconds { get; set; }
        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; set; }
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
            ProcessorQueueSize = newConfig.ProcessorQueueSize;
            ProcessorWaitTimeWhenQueueEmpty_InMilliseconds = newConfig.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds;
            NodeId = newConfig.NodeId;
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
            ConfigurationChangeHandleTimeout_InMilliseconds = newConfig.ConfigurationChangeHandleTimeout_InMilliseconds;
            AppendEntriesTimeoutOnReceive_InMilliseconds = newConfig.AppendEntriesTimeoutOnReceive_InMilliseconds;
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



