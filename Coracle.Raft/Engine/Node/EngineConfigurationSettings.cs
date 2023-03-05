#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;

namespace Coracle.Raft.Engine.Node
{
    public class EngineConfigurationSettings : IEngineConfiguration
    {
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
        public Uri NodeUri { get; set; }
        public int EntryCommitWaitTimeout_InMilliseconds { get; set; }
        public int EntryCommitWaitInterval_InMilliseconds { get; set; }
        public int CatchUpOfNewNodesTimeout_InMilliseconds { get; set; }
        public int CatchUpOfNewNodesWaitInterval_InMilliseconds { get; set; }
        public int CheckDepositionWaitInterval_InMilliseconds { get; set; }
        public int InstallSnapshotChunkTimeoutOnSend_InMilliseconds { get; set; }
        public int InstallSnapshotChunkTimeoutOnReceive_InMilliseconds { get; set; }
        public int CompactionAttemptTimeout_InMilliseconds { get; set; }
        public int CompactionAttemptInterval_InMilliseconds { get; set; }
        public int CompactionWaitPeriod_InMilliseconds { get; set; }
        public int SnapshotThresholdSize { get; set; }
        public int SnapshotBufferSizeFromLastEntry { get; set; }

        public void ApplyFrom(IEngineConfiguration newConfig)
        {
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
            NodeUri = newConfig.NodeUri;
            EntryCommitWaitTimeout_InMilliseconds = newConfig.EntryCommitWaitTimeout_InMilliseconds;
            EntryCommitWaitInterval_InMilliseconds = newConfig.EntryCommitWaitInterval_InMilliseconds;
            CatchUpOfNewNodesTimeout_InMilliseconds = newConfig.CatchUpOfNewNodesTimeout_InMilliseconds;
            CatchUpOfNewNodesWaitInterval_InMilliseconds = newConfig.CatchUpOfNewNodesWaitInterval_InMilliseconds;
            CheckDepositionWaitInterval_InMilliseconds = newConfig.CheckDepositionWaitInterval_InMilliseconds;
            InstallSnapshotChunkTimeoutOnSend_InMilliseconds = newConfig.InstallSnapshotChunkTimeoutOnSend_InMilliseconds;
            InstallSnapshotChunkTimeoutOnReceive_InMilliseconds = newConfig.InstallSnapshotChunkTimeoutOnReceive_InMilliseconds;
            CompactionAttemptTimeout_InMilliseconds = newConfig.CompactionAttemptTimeout_InMilliseconds;
            CompactionAttemptInterval_InMilliseconds = newConfig.CompactionAttemptInterval_InMilliseconds;
            CompactionWaitPeriod_InMilliseconds = newConfig.CompactionWaitPeriod_InMilliseconds;
            SnapshotThresholdSize = newConfig.SnapshotThresholdSize;
            SnapshotBufferSizeFromLastEntry = newConfig.SnapshotBufferSizeFromLastEntry;
        }
    }
}
