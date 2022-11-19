using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;

namespace Core.Raft.Canoe.Engine.ClientHandling
{
    /// <summary>
    /// Instances of this class will hold the resultant expected TCommandResult and other information helpful with the operation.
    /// </summary>
    public class ClientHandlingResult //: ICommandResult
    {
        public ClientHandlingResult()
        {

        }

        public bool IsOperationSuccessful { get; set; }

        public INodeConfiguration LeaderNodeConfiguration { get; set; }

        public Exception Exception { get; set; }

        public object CommandResult { get; set; }

        public ICommand OriginalCommand { get; set; }
    }
}
