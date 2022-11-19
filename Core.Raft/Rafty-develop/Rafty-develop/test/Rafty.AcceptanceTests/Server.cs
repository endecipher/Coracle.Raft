using Rafty.Concensus;
using Rafty.FiniteStateMachine;
using Rafty.Log;

namespace Rafty.AcceptanceTests
{
    using Concensus.Node;

    public class Server
    {
        public Server(InMemoryLog log, InMemoryStateMachine fsm, Node node)
        {
            this.Log = log;
            this.Fsm = fsm;
            this.Node = node;
        }
        public InMemoryLog Log { get; private set; }
        public InMemoryStateMachine Fsm { get; private set; }
        public Node Node { get; private set; }
    }
}