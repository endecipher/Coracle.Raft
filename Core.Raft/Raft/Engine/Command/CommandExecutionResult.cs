using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Operational;

namespace Coracle.Raft.Engine.Command
{
    public sealed class CommandExecutionResult : BaseOperationResult
    {
        public CommandExecutionResult()
        {

        }

        public INodeConfiguration LeaderNodeConfiguration { get; set; }

        public object CommandResult { get; set; }

        public ICommand OriginalCommand { get; set; }
    }
}
