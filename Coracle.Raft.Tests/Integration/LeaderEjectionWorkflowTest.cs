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

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.States;
using EntityMonitoring.FluentAssertions.Structure;
using FluentAssertions;
using Xunit;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Command;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Tests.Framework;
using Coracle.Raft.Tests.Components.Helper;
using Coracle.Raft.Engine.States.LeaderEntities;
using Coracle.Raft.Examples.Data;
using Coracle.Raft.Tests.Components.Remoting;

namespace Coracle.Raft.Tests.Integration
{
    /// <summary>
    /// Tests configuration change requests which doesn't include the leader node as of that point
    /// </summary>
    [TestCaseOrderer($"Coracle.Raft.Tests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.Raft.Tests")]
    public class LeaderEjectionWorkflowTest : BaseTest, IClassFixture<TestContext>
    {
        public LeaderEjectionWorkflowTest(TestContext context) : base(context)
        {
        }

        [Fact]
        [Order(1)]
        public async Task IsTurningToLeader()
        {
            await EstablishSampleLeader();
        }

        [Fact]
        [Order(2)]
        public async Task IsLeaderHandlingConfigurationChangeAndSendingSnapshot()
        {
            #region Arrange

            var notifiableQueue = CaptureActivities();

            var configurationChanged = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(OnConfigurationChangeRequestReceive.ActionName, OnConfigurationChangeRequestReceive.ConfigurationChangeSuccessful)).RemoveOnceMatched();

            var nodesCaughtUp = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(OnCatchUpOfNewlyAddedNodes.ActionName, OnCatchUpOfNewlyAddedNodes.NodesCaughtUp)).RemoveOnceMatched();

            var decommissioning = notifiableQueue
                .AttachNotifier(x =>
                    x.Is(AbstractStateActivityConstants.Entity, AbstractStateActivityConstants.Decommissioning)).RemoveOnceMatched();

            var currentConfiguration = Context.GetService<IClusterConfiguration>().CurrentConfiguration;

            var newConfiguration = currentConfiguration.Select(x => new NodeConfiguration
            {
                UniqueNodeId = x.UniqueNodeId

            }).Append(new NodeConfiguration
            {
                UniqueNodeId = MockNewNodeC //Adding NewNodeC

            }).Where(x => !x.UniqueNodeId.Equals(SUT)) //Removing SUT (Which is leader)
            .ToList();

            CreateMockNode(MockNewNodeC);

            #endregion

            #region Act

            Exception caughtException = null;
            ConfigurationChangeResult changeResult = null;
            StateCapture captureBeforeConfigurationChange = null, captureAfterDecommission = null;

            try
            {
                captureBeforeConfigurationChange = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());

                /// <remarks>
                /// Enqueuing Responses for old nodes
                /// </remarks>
                EnqueueAppendEntriesSuccessResponse(MockNodeA); //For Replication Approval of C-old,new
                EnqueueAppendEntriesSuccessResponse(MockNodeB); //For Replication Approval of C-old,new

                EnqueueAppendEntriesSuccessResponse(MockNodeA); //For Replication Approval of C-new
                EnqueueAppendEntriesSuccessResponse(MockNodeB); //For Replication Approval of C-new

                EnqueueAppendEntriesSuccessResponse(MockNodeA); //For any heartbeats being sent for the C-new entry to be replicated and then applying cluster configuration
                EnqueueAppendEntriesSuccessResponse(MockNodeB); //For any heartbeats being sent for the C-new entry to be replicated and then applying cluster configuration

                // New Node may respond with Success false, as it has just started up and needs replication
                Context.NodeContext.GetMockNode(MockNewNodeC).EnqueueNextAppendEntriesResponse(rpc => new AppendEntriesRPCResponse
                {
                    Term = rpc.Term,
                    FirstIndexOfConflictingEntryTermOnFailure = rpc.PreviousLogIndex,
                    ConflictingEntryTermOnFailure = rpc.PreviousLogTerm,
                    Success = false
                }, approveImmediately: true);

                EnqueueAppendEntriesSuccessResponse(MockNewNodeC); //Enqueue Success AppendEntries for the remaining entries
                EnqueueAppendEntriesSuccessResponse(MockNewNodeC); //For any heartbeats being sent for the C-new entry to be replicated and then applying cluster configuration

                changeResult = await Context.GetService<IConfigurationRequestExecutor>().IssueChange(new ConfigurationChangeRequest
                {
                    UniqueRequestId = Guid.NewGuid().ToString(),
                    NewConfiguration = newConfiguration

                }, CancellationToken.None);

                nodesCaughtUp.Wait(EventNotificationTimeOut);

                configurationChanged.Wait(EventNotificationTimeOut);

                decommissioning.Wait(EventNotificationTimeOut);

                captureAfterDecommission = new StateCapture(Context.GetService<ICurrentStateAccessor>().Get());
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assert

            var assertableQueue = StartAssertions(notifiableQueue);

            caughtException
                .Should().Be(null, $" ");

            captureBeforeConfigurationChange
                .StateValue
                .Should().Be(StateValues.Leader, "As the leader state is has Leader value");

            var lastIndex = await Context.GetService<IPersistentStateHandler>().GetLastIndex();

            captureAfterDecommission
                .CommitIndex
                .Should().Be(lastIndex, "The conf C-new entry should have been replicated to other nodes and also committed");

            captureAfterDecommission
                .LastApplied
                .Should().Be(lastIndex, "The conf C-new entry should have been replicated to other nodes and also committed");

            captureAfterDecommission
                .MatchIndexes
                .Values
                .Should()
                .Match((i) => i.All(_ => _.Equals(lastIndex)),
                    $"{MockNodeA} and {MockNodeB} and {MockNewNodeC} should have had replicated all entries up until the leader's last log index");

            captureAfterDecommission
                .NextIndexes
                .Values
                .Should()
                .Match((i) => i.All(_ => _.Equals(lastIndex + 1)),
                    $"{MockNodeA} and {MockNodeB} and {MockNewNodeC} should have had replicated all entries up until the leader's last log index, and the nextIndex to send for each peer mock node should be one greater");

            captureAfterDecommission
                .StateValue
                .Should().Be(StateValues.Abandoned, "As the leader state is now decommissioned");

            Cleanup();
            #endregion
        }
    }
}
