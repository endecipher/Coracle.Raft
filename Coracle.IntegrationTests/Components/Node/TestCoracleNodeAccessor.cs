using Coracle.IntegrationTests.Components.Helper;
using Coracle.Raft.Engine.Node;

namespace Coracle.IntegrationTests.Components.Node
{
    public interface ICoracleNodeAccessor
    {
        ICoracleNode CoracleNode { get; }

        bool IsInitialized => CoracleNode != null;
    }

    public class TestCoracleNodeAccessor : ICoracleNodeAccessor
    {
        private object _lock = new object();
        private ICoracleNode _node = null;

        public ICoracleNode CoracleNode
        {

            get
            {
                lock (_lock)
                {
                    return _node;
                }
            }

            private set
            {
                lock (_lock)
                {
                    _node = value;
                    _node.InitializeConfiguration();
                    _node.Start();
                }
            }
        }

        public TestCoracleNodeAccessor(IEngineConfiguration engineConfig, ICoracleNode node)
        {
            CoracleNode = node;
        }
    }
}
