using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Threading;

namespace Coracle.Raft.Engine.Node
{
    internal sealed class CanoeNode : ICanoeNode
    {
        #region Constants

        public const string CanoeNodeEntity = nameof(CanoeNode);
        public const string StateChange = nameof(StateChange);
        public const string NewState = nameof(NewState);
        public const string InvocableActions = nameof(InvocableActions);
        public const string ShouldExecuteSeparately = nameof(ShouldExecuteSeparately);
        public const string EventKey = nameof(EventKey);

        #endregion

        IActivityLogger ActivityLogger { get; }
        IResponsibilities Responsibilities { get; }
        IDiscoverer Discoverer { get; }
        IClusterConfigurationChanger ClusterConfigurationChanger { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IStateChanger StateChanger { get; }
        IEngineConfiguration EngineConfiguration { get; }
        ITaskProcessorConfiguration TaskProcessorConfiguration { get; }

        public CanoeNode(
            IActivityLogger activityLogger,
            IResponsibilities responsibilities,
            IDiscoverer discoverer,
            IClusterConfigurationChanger clusterConfigurationChanger,
            ICurrentStateAccessor currentStateAccessor,
            IStateChanger stateChanger,
            IEngineConfiguration engineConfiguration,
            ITaskProcessorConfiguration processorConfiguration)
        {
            ActivityLogger = activityLogger;
            Responsibilities = responsibilities;
            Discoverer = discoverer;
            ClusterConfigurationChanger = clusterConfigurationChanger;
            CurrentStateAccessor = currentStateAccessor;
            StateChanger = stateChanger;
            EngineConfiguration = engineConfiguration;
            TaskProcessorConfiguration = processorConfiguration;
        }

        //Should be Transient
        //public IExternalRpcHandler ExternalRpcHandler => ComponentContainer.Instance.GetInstance<IExternalRpcHandler>();
        //public IExternalClientCommandHandler ExternalClientCommandHandler => ComponentContainer.Instance.GetInstance<IExternalClientCommandHandler>();
        //public IDiscoverer Discoverer => ComponentContainer.Instance.GetInstance<IDiscoverer>();

        public bool IsStarted { get; private set; } = false;
        public bool IsInitialized { get; private set; } = false;

        public void InitializeConfiguration()
        {
            if (IsStarted) throw new InvalidOperationException(nameof(IsStarted) + "already true");

            IDiscoveryOperation operation = Discoverer.EnrollThisNode(EngineConfiguration.DiscoveryServerUri, new NodeConfiguration
            {
                UniqueNodeId = EngineConfiguration.NodeId,
                BaseUri = EngineConfiguration.ThisNodeUri
            },
            CancellationToken.None)
                .GetAwaiter().GetResult();

            if (operation.IsOperationSuccessful)
            {
                Thread.Sleep(EngineConfiguration.WaitPostEnroll_InMilliseconds);

                operation = Discoverer.GetAllNodes(EngineConfiguration.DiscoveryServerUri, CancellationToken.None).GetAwaiter().GetResult();

                if (operation.IsOperationSuccessful)
                {
                    var config = TaskProcessorConfiguration as TaskProcessorConfiguration;

                    config.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds = EngineConfiguration.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds;
                    config.ProcessorQueueSize = EngineConfiguration.ProcessorQueueSize;

                    ClusterConfigurationChanger.ApplyConfiguration(new ClusterMembershipChange
                    {
                        Configuration = operation.AllNodes,
                        ConfigurationLogEntryIndex = default,
                    });

                    IsInitialized = true;
                    return;
                }
            }

            throw operation.Exception;
        }


        /// <summary>
        /// Starts the Canoe Node Engine Operations.
        /// All Session variables and Registries are scanned here.
        /// New <see cref="IResponsibilities"/> processor queue is also configured.
        /// Default starting <see cref="Follower"/> state is initialized.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Start()
        {
            if (!IsInitialized) throw new InvalidOperationException($"{nameof(InitializeConfiguration)} not called");

            if (IsStarted) throw new InvalidOperationException($"{nameof(Start)} called already");

            //New Responsibilities
            Responsibilities.ConfigureNew(identifier: EngineConfiguration.NodeId);

            //Validate if all is okay

            //Start Engine
            StateChanger.Initialize();

            IsStarted = true;
        }

        public void Pause()
        {
            if (!IsStarted) throw new InvalidOperationException(nameof(IsStarted) + "not true");

            CurrentStateAccessor.Get()?.Pause();
        }

        public void Resume()
        {
            if (!IsStarted) throw new InvalidOperationException(nameof(IsStarted) + "not true");

            CurrentStateAccessor.Get()?.Resume();
        }
    }
}
