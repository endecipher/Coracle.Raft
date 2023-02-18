using ActivityLogger.Logging;
using ActivityLogLevel = ActivityLogger.Logging.ActivityLogLevel;
using Newtonsoft.Json;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Logs;
using Coracle.Samples.Logging;

namespace Coracle.Samples.PersistentData
{
    public class Element
    {
        public LogEntry Entry { get; set; }

        public Element NextElement { get; set; }

        public Element PreviousElement { get; set; }
    }
}
