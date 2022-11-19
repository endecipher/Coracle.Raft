using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.States.LeaderState;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal interface IAppendEntriesRPCContext : IActionContext
    {
        INodeConfiguration NodeConfiguration
        {
            get;
        }

        int CurrentRetryCounter { get; } 
        DateTimeOffset InvocationTime { get; }
        AppendEntriesSession Session { get; }
    }
}
