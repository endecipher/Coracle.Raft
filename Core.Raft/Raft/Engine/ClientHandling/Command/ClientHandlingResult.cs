using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;

namespace Coracle.Raft.Engine.ClientHandling.Command
{
    /// <summary>
    /// Instances of this class will hold the resultant expected TCommandResult and other information helpful with the operation.
    /// </summary>
    public sealed class ClientHandlingResult : OperationalResult
    {
        public ClientHandlingResult()
        {

        }

        public INodeConfiguration LeaderNodeConfiguration { get; set; }

        public object CommandResult { get; set; }

        public ICommand OriginalCommand { get; set; }
    }
}
