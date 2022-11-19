﻿using ActivityLogger.Logging;
using Coracle.Web.Discovery.Coracle.Logging;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Discovery.Registrar;
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
        public async Task AlertAllNodesForRefresh(CancellationToken cancellationToken)
        {
            var allNodes = await NodeRegistry.GetAll();

            ActivityLogger?.Log(new DiscoveryImplActivity
            {
                EntitySubject = NodeRegistrarEntity,
                Event = AlertingAll,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(Nodes, allNodes)) 
            .WithCallerInfo());

            foreach (var node in allNodes)
            {
                var client = HttpClientFactory.CreateClient();

                await client.GetAsync(new Uri(node.BaseUri, $"raft/RefreshDiscovery"), cancellationToken);
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
                ActivityLogger?.Log(new DiscoveryImplActivity
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
                ActivityLogger?.Log(new DiscoveryImplActivity
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
