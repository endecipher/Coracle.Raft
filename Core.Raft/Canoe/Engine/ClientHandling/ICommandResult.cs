using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;

namespace Core.Raft.Canoe.Engine.ClientHandling
{
    public interface ICommandResult
    {
        Exception Exception { get; }
        bool IsOperationSuccessful { get; }
        INodeConfiguration LeaderNodeConfiguration { get; }
        ICommand OriginalCommand { get; }
        object CommandResult { get; }
    }
}
