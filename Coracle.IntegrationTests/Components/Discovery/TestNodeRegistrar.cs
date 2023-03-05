using ActivityLogger.Logging;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Examples.Registrar;
using Coracle.Raft.Examples.Logging;

namespace Coracle.Raft.Tests.Components.Discovery
{
    public class TestNodeRegistrar : INodeRegistrar
    {
        #region Constants

        public const string NodeRegistrarEntity = nameof(TestNodeRegistrar);
        public const string EnrollingNew = nameof(EnrollingNew);
        public const string GetAll = nameof(GetAll);
        public const string AlertingAll = nameof(AlertingAll);
        public const string Node = nameof(Node);
        public const string Nodes = nameof(Nodes);
        public const string Result = nameof(Result);

        #endregion

        public TestNodeRegistrar(INodeRegistry nodeRegistry, IActivityLogger activityLogger)
        {
            NodeRegistry = nodeRegistry;
            ActivityLogger = activityLogger;
        }

        public INodeRegistry NodeRegistry { get; }
        public IActivityLogger ActivityLogger { get; }

        public async Task Clear()
        {
            var allNodes = await NodeRegistry.GetAll();

            foreach (var node in allNodes)
            {
                await NodeRegistry.TryRemove(node.UniqueNodeId);
            }
        }

        public async Task<DiscoveryResult> Enroll(NodeConfiguration configuration, CancellationToken cancellationToken)
        {
            var res = new DiscoveryResult();

            try
            {
                await NodeRegistry.AddOrUpdate(configuration);
                res.IsSuccessful = true;
            }
            catch (Exception ex)
            {
                res.IsSuccessful = false;
                res.Exception = ex;
            }
            finally
            {
                ActivityLogger?.Log(new ImplActivity
                {
                    EntitySubject = NodeRegistrarEntity,
                    Event = EnrollingNew,
                    Level = res.IsSuccessful ? ActivityLogLevel.Debug : ActivityLogLevel.Error,
                }
                .With(ActivityParam.New(Node, configuration))
                .WithCallerInfo());
            }

            return res;
        }

        public async Task<DiscoveryResult> GetAllNodes(CancellationToken cancellationToken)
        {
            var res = new DiscoveryResult();

            try
            {
                res.AllNodes = await NodeRegistry.GetAll();
                res.IsSuccessful = true;
            }
            catch (Exception ex)
            {
                res.IsSuccessful = false;
                res.Exception = ex;
            }
            finally
            {
                ActivityLogger?.Log(new ImplActivity
                {
                    EntitySubject = NodeRegistrarEntity,
                    Event = GetAll,
                    Level = res.IsSuccessful ? ActivityLogLevel.Debug : ActivityLogLevel.Error,
                }
                .With(ActivityParam.New(Result, res))
                .WithCallerInfo());
            }

            return res;
        }
    }
}
