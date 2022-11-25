using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal interface IAppendEntriesRPCContext : IActionContext
    {
        /// Target Node Configuration
        INodeConfiguration NodeConfiguration
        {
            get;
        }

        /// Invocation Time for the Event Action
        DateTimeOffset InvocationTime { get; }
    }
}
