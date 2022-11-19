using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Actions.Awaiters;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using EventGuidance.Responsibilities;

namespace Core.Raft.Canoe.Engine.Node
{
    /// <summary>
    /// Represents a Coracle Node
    /// 
    /// This node and all actions that follow post initialization and starting, internally use the dependencies:
    /// <list type="bullet">
    /// <item><see cref="IDiscoverer"/> for service discovery operations</item>
    /// <item><see cref="IResponsibilities"/> for parallel task processing</item>
    /// <item><see cref="IPersistentProperties"/> for persistent state manipulations</item>
    /// <item><see cref="IStateChanger"/> for RAFT state manipulations</item>
    /// <item><see cref="IRemoteManager"/> for outbound RAFT RPCs, or forwarding of Client Commands to other nodes</item>
    /// <item><see cref="IExternalRpcHandler"/> for handling inbound RAFT RPCs towards this current <see cref="ICanoeNode"></item>
    /// <item><see cref="IClientRequestHandler"/> for processing client commands and applying them to State Machine</item>
    /// <item><see cref="IExternalClientCommandHandler"/> for handling inbound commands from the client</item>
    /// <item><see cref="IGlobalAwaiter"/> for handling multiple <see cref="IActionLock"> facilitiating wait functionality across Node Actions</item>
    /// </list>
    /// 
    /// 
    /// </summary>
    public interface ICanoeNode 
    {
        #region Engine Methods 

        bool IsStarted { get; }

        void Start();

        void Pause();

        void Resume();

        #endregion

        #region Initialize

        bool IsInitialized { get; }

        void InitializeConfiguration();

        #endregion
    }
}
