using TaskGuidance.BackgroundProcessing.Core;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Actions.Awaiters;
using TaskGuidance.BackgroundProcessing.Actions;

namespace Coracle.Raft.Engine.Node
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
