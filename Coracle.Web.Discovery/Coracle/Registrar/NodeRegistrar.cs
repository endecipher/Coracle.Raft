using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.Discovery.Registrar;
using Coracle.Samples.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Discovery.Coracle.Registrar
{
    public class NodeRegistrar : INodeRegistrar
    {
        #region Constants

        public const string NodeRegistrarEntity = nameof(NodeRegistrar);
        public const string EnrollingNew = nameof(EnrollingNew);
        public const string GetAll = nameof(GetAll);
        public const string AlertingAll = nameof(AlertingAll);
        public const string Node = nameof(Node);
        public const string Nodes = nameof(Nodes);
        public const string Result = nameof(Result);

        #endregion

        public NodeRegistrar(INodeRegistry nodeRegistry, IHttpClientFactory httpClientFactory, IActivityLogger activityLogger) //Add HttpClientFactory and check if it is a transient
        {
            NodeRegistry = nodeRegistry;
            HttpClientFactory = httpClientFactory;
            ActivityLogger = activityLogger;
        }

        public INodeRegistry NodeRegistry { get; }
        public IHttpClientFactory HttpClientFactory { get; }
        public IActivityLogger ActivityLogger { get; }

        /// <summary>
        /// Make it a setting
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Clear()
        {
            var allNodes = await NodeRegistry.GetAll();

            foreach (var node in allNodes)
            {
                await NodeRegistry.TryRemove(node.UniqueNodeId);
            }
        }

        public async Task<IDiscoveryOperation> Enroll(NodeConfiguration configuration, CancellationToken cancellationToken)
        {
            var res = new DiscoveryOperation();

            try
            {
                await NodeRegistry.AddOrUpdate(configuration);
                res.IsOperationSuccessful = true;

                //AlertAllNodesForRefresh(cancellationToken).Start();
            }
            catch (Exception ex)
            {
                res.IsOperationSuccessful = false;
                res.Exception = ex;
            }
            finally
            {
                ActivityLogger?.Log(new ImplActivity
                {
                    EntitySubject = NodeRegistrarEntity,
                    Event = EnrollingNew,
                    Level = res.IsOperationSuccessful ? ActivityLogLevel.Debug : ActivityLogLevel.Error,
                }
                .With(ActivityParam.New(Node, configuration))
                .WithCallerInfo());
            }

            return res;
        }

        public async Task<IDiscoveryOperation> GetAllNodes(CancellationToken cancellationToken)
        {
            var res = new DiscoveryOperation();

            try
            {
                res.AllNodes = await NodeRegistry.GetAll();
                res.IsOperationSuccessful = true;

                
            }
            catch (Exception ex)
            {
                res.IsOperationSuccessful = false;
                res.Exception = ex;
            }
            finally
            {
                ActivityLogger?.Log(new ImplActivity
                {
                    EntitySubject = NodeRegistrarEntity,
                    Event = GetAll,
                    Level = res.IsOperationSuccessful ? ActivityLogLevel.Debug : ActivityLogLevel.Error,
                }
                .With(ActivityParam.New(Result, res))
                .WithCallerInfo());
            }

            return res;
        }

        private IActivity GetActivity([FromServices] IActivity newC) => newC;
    }
}
