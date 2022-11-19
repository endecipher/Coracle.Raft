using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Discovery
{
    /// <summary>
    /// Base class for implementing Transient Service Discovery
    /// </summary>
    public abstract class BaseDiscoverer : IDiscoverer
    {
        #region Constants

        public const string BaseDiscovererEntity = nameof(BaseDiscoverer);
        public const string DiscoveryFailed = nameof(DiscoveryFailed);
        public const string Exception = nameof(Exception);

        #endregion

        public BaseDiscoverer(IActivityLogger activityLogger, IEngineConfiguration engineConfiguration, IClusterConfiguration clusterConfiguration)
        {
            ActivityLogger = activityLogger;
            EngineConfiguration = engineConfiguration;
            ClusterConfiguration = clusterConfiguration;
        }

        protected abstract bool ThrowOnException { get; }
        protected IActivityLogger ActivityLogger { get; }
        protected IEngineConfiguration EngineConfiguration { get; }
        protected IClusterConfiguration ClusterConfiguration { get; }

        public async Task RefreshDiscovery()
        {
            var op = await GetAllNodes(EngineConfiguration.DiscoveryServerUri, new CancellationToken());

            if (op.IsOperationSuccessful)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"GetAllNodes failed",
                    EntitySubject = BaseDiscovererEntity,
                    Event = DiscoveryFailed,
                    Level = ActivityLogLevel.Error,

                }
                .With(ActivityParam.New(Exception, op.Exception))
                .WithCallerInfo());

                ClusterConfiguration.UpdateConfiguration(EngineConfiguration.NodeId, op.AllNodes);

                return;
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                Description = $"GetAllNodes failed",
                EntitySubject = BaseDiscovererEntity,
                Event = DiscoveryFailed,
                Level = ActivityLogLevel.Error,

            }
            .With(ActivityParam.New(Exception, op.Exception))
            .WithCallerInfo());

            throw op.Exception;
        }

        public abstract Task<IDiscoveryOperation> EnrollThisNode(Uri registrarUri, INodeConfiguration configuration, CancellationToken cancellationToken);
        public abstract Task<IDiscoveryOperation> GetAllNodes(Uri registrarUri, CancellationToken cancellationToken);
    }
}
