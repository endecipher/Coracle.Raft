using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Operational;

namespace Core.Raft.Canoe.Engine.ClientHandling
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
