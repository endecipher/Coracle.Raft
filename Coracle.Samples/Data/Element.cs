using Coracle.Raft.Engine.Logs;

namespace Coracle.Raft.Examples.Data
{
    public class Element
    {
        public LogEntry Entry { get; set; }

        public Element NextElement { get; set; }

        public Element PreviousElement { get; set; }
    }
}
