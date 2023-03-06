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
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;
using System.Threading;
using Coracle.Raft.Engine.ActivityLogger;
using Coracle.Raft.Engine.Node.Validations;
using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Exceptions;

namespace Coracle.Raft.Engine.Node
{
    internal sealed class CoracleNode : ICoracleNode
    {
        #region Constants

        public const string Entity = nameof(CoracleNode);
        public const string Initialized = nameof(Initialized);
        public const string Started = nameof(Started);
        public const string ValidationFailed = nameof(ValidationFailed);
        public const string NodeNotPartOfCluster = nameof(NodeNotPartOfCluster);
        public const string errors = nameof(errors);
        public const string nodeId = nameof(nodeId);

        #endregion

        IActivityLogger ActivityLogger { get; }
        IResponsibilities Responsibilities { get; }
        IDiscoveryHandler Discoverer { get; }
        IMembershipChanger ClusterConfigurationChanger { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IStateChanger StateChanger { get; }
        IEngineConfiguration EngineConfiguration { get; }
        ITaskProcessorConfiguration TaskProcessorConfiguration { get; }
        IEngineValidator ConfigurationValidator { get; }

        public CoracleNode(
            IActivityLogger activityLogger,
            IResponsibilities responsibilities,
            IDiscoveryHandler discoverer,
            IMembershipChanger clusterConfigurationChanger,
            ICurrentStateAccessor currentStateAccessor,
            IStateChanger stateChanger,
            IEngineConfiguration engineConfiguration,
            ITaskProcessorConfiguration processorConfiguration,
            IEngineValidator configurationValidator)
        {
            ActivityLogger = activityLogger;
            Responsibilities = responsibilities;
            Discoverer = discoverer;
            ClusterConfigurationChanger = clusterConfigurationChanger;
            CurrentStateAccessor = currentStateAccessor;
            StateChanger = stateChanger;
            EngineConfiguration = engineConfiguration;
            TaskProcessorConfiguration = processorConfiguration;
            ConfigurationValidator = configurationValidator;
        }

        public bool IsStarted { get; private set; } = false;
        public bool IsInitialized { get; private set; } = false;

        public void InitializeConfiguration()
        {
            if (IsStarted) throw CoracleNodeAlreadyStartedException.New(EngineConfiguration.NodeId);

            /// Validate Engine Configuration 
            if (!ConfigurationValidator.IsValid(EngineConfiguration, out var errorMessages))
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = ValidationFailed,
                    Level = ActivityLogLevel.Error
                }
                .With(ActivityParam.New(nodeId, EngineConfiguration.NodeId))
                .With(ActivityParam.New(errors, errorMessages))
                .WithCallerInfo());

                throw EngineConfigurationInvalidException.New(EngineConfiguration.NodeId, errorMessages);
            }

            var config = TaskProcessorConfiguration as TaskProcessorConfiguration;

            config.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds = EngineConfiguration.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds;
            config.ProcessorQueueSize = EngineConfiguration.ProcessorQueueSize;

            var operation = Discoverer.Enroll(new NodeConfiguration
            {
                UniqueNodeId = EngineConfiguration.NodeId,
                BaseUri = EngineConfiguration.NodeUri
            },
            CancellationToken.None).GetAwaiter().GetResult();

            if (operation.IsSuccessful)
            {
                Thread.Sleep(EngineConfiguration.WaitPostEnroll_InMilliseconds);

                operation = Discoverer.GetAllNodes(CancellationToken.None).GetAwaiter().GetResult();

                if (operation.IsSuccessful)
                {
                    ClusterConfigurationChanger.ChangeMembership(new MembershipUpdateEvent
                    {
                        Configuration = operation.AllNodes,
                        ConfigurationLogEntryIndex = default,
                    });

                    /// Validate Cluster Configuration, since current node must be present
                    if (!ClusterConfigurationChanger.IsThisNodePartOfCluster)
                    {
                        ActivityLogger?.Log(new CoracleActivity
                        {
                            EntitySubject = Entity,
                            Event = NodeNotPartOfCluster,
                            Level = ActivityLogLevel.Error
                        }
                        .With(ActivityParam.New(nodeId, EngineConfiguration.NodeId))
                        .WithCallerInfo());

                        throw InvalidDiscoveryException.New(EngineConfiguration.NodeId);
                    }

                    IsInitialized = true;

                    ActivityLogger?.Log(new CoracleActivity
                    {
                        EntitySubject = Entity,
                        Event = Initialized,
                        Level = ActivityLogLevel.Information
                    }
                    .With(ActivityParam.New(nodeId, EngineConfiguration.NodeId))
                    .WithCallerInfo());

                    return;
                }
            }

            throw operation.Exception;
        }


        
        public void Start()
        {
            if (!IsInitialized) throw CoracleNodeNotInitializedException.New(EngineConfiguration.NodeId);

            if (IsStarted) throw CoracleNodeAlreadyStartedException.New(EngineConfiguration.NodeId);

            /// Setup new Responsibilities
            Responsibilities.ConfigureNew(identifier: EngineConfiguration.NodeId);

            /// Start Engine
            StateChanger.Initialize();

            IsStarted = true;

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = Started,
                Level = ActivityLogLevel.Information
            }
            .With(ActivityParam.New(nodeId, EngineConfiguration.NodeId))
            .WithCallerInfo());
        }

        public void Pause()
        {
            if (!IsStarted) throw CoracleNodeNotStartedException.New(EngineConfiguration.NodeId);

            CurrentStateAccessor.Get()?.Stop();
        }

        public void Resume()
        {
            if (!IsStarted) throw CoracleNodeNotStartedException.New(EngineConfiguration.NodeId);

            CurrentStateAccessor.Get()?.Resume();
        }
    }
}
