using Coracle.Raft.Engine.Configuration.Cluster;
using System;

namespace Coracle.Raft.Engine.Actions.Contexts
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
