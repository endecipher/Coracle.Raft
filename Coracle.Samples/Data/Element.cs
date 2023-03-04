using Coracle.Raft.Engine.Logs;

namespace Coracle.Samples.Data
{
    public class Element
    {
        public LogEntry Entry { get; set; }

        public Element NextElement { get; set; }

        public Element PreviousElement { get; set; }
    }
}
