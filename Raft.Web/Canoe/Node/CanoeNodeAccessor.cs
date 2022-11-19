using Core.Raft.Canoe.Engine.Node;
using EventGuidance.Dependency;

namespace Raft.Web.Canoe.Node
{
    public interface INode
    {
        ICanoeNode CanoeNode { get; set; }

        void Initialize();
    }

    public class CanoeNodeAccessor : INode
    {
        private object obj =  new object();

        private ICanoeNode _node = null;

        public ICanoeNode CanoeNode { 
            get
            {
                lock (obj)
                {
                    return _node;
                }
            }
            set
            {
                lock (obj)
                {
                    _node = value;
                }
            }
        }

        public CanoeNodeAccessor()
        {
            CanoeNode = ComponentContainer.Instance.GetInstance<ICanoeNode>();
        }

        public void Initialize()
        {
            lock (obj)
            {
                CanoeNode.Start();
            }
        }
    }
}
