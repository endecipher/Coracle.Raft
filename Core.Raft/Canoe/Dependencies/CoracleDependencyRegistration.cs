using Core.Raft.Canoe.Engine.Actions.Awaiters;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Discovery.Registrar;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Dependency;

namespace Core.Raft.Canoe.Dependencies
{
    /// <summary>
    /// The following dependencies which handle extensible logic need an external implementation to be registered for Constructor Dependecy Injection -
    /// <list type="bullet">
    /// <item><see cref = "IClientRequestHandler" /> - For command operations</item>
    /// <item><see cref = "IRemoteManager" /> - For outbound RPC Calls</item>
    /// <item><see cref = "IPersistentProperties" /> - For holding the node's persistent state and entries</item>
    /// <item><see cref = "IDiscoverer" /> - For discovery of other Coracle nodes</item>
    /// </list>
    /// 
    /// <para/>
    /// For Service Discovery, the Registrar server can register and external implementation for the below interfaces for ease of use - 
    /// <list type="bullet">
    /// <item><see cref = "INodeRegistrar" /></item>
    /// <item><see cref = "INodeRegistry" /></item>
    /// </list>
    /// 
    /// <para/>
    /// The implementation <see cref="EngineConfigurationSettings"/> of the interface <see cref="IEngineConfiguration"/> must be modified externally before <see cref="ICanoeNode.InitializeConfiguration"/> is called.
    /// 
    /// <para/>
    /// <see cref="EngineRegistry.Register(IDependencyContainer)"/> must be called during Dependency Injection Container Setup to register necessary internal implementations. 
    /// 
    /// <para/>
    /// The server implementating <see cref="ICanoeNode"/> may call on the below via DI for Coracle Node operations - 
    /// <list type="bullet">
    /// <item><see cref = "IExternalClientCommandHandler" /> - For handling inbound client commands </item>
    /// <item><see cref = "INodeConfiguration" /> - For creating a <see cref="NodeConfiguration"/> POCO </item>
    /// <item><see cref = "IClusterConfiguration" /> - For seeing the cluster nodes identified and being communicated with. //TODO: MAke sure ConcurrentDictionary cannot be added to from outside</item>
    /// <item><see cref = "ICanoeNode" /> - For initialization and system operations on the Coracle Node</item>
    /// <item><see cref = "IExternalRpcHandler" /> - For handling inbound RPC calls from other Coracle nodes</item>
    /// <item><see cref = "ICurrentStateAccessor" /> - For holding the Current State used by the entire application</item>
    /// </list>
    /// 
    /// </summary>
    public class CoracleDependencyRegistration : IDependencyRegistration
    {
        public void Register(IDependencyContainer container)
        {
            container.RegisterTransient<IExternalClientCommandHandler, ExternalClientCommandHandler>();
            container.RegisterSingleton<INodeConfiguration, NodeConfiguration>(); //It's not a dependency, but anyone who needs a blank template of NodeConfiguration can use it.
            container.RegisterSingleton<IClusterConfiguration, ClusterConfiguration>(); //Will store in memory the current nodes in the cluster for any operations
            container.RegisterSingleton<ILeaderNodePronouncer, LeaderNodePronouncer>(); //Stores information about the recognized leader, if needed, forwarding can occur for commands to that leader
            container.RegisterSingleton<IElectionTimer, ElectionTimer>();
            container.RegisterSingleton<IHeartbeatTimer, HeartbeatTimer>();
            container.RegisterSingleton<ICanoeNode, CanoeNode>(); //No need for client implementation. This regiatration should take care. ICanoeNodeState should be internal, and should not be exposed for State Manipulations
            container.RegisterTransient<IExternalRpcHandler, ExternalRemoteProcedureHandler>(); //Internal Iplementation, but Public interface, 
            container.RegisterSingleton<IGlobalAwaiter, GlobalAwaiter>(); //All internal. No need for it to be called 
            container.RegisterSingleton<IEngineConfiguration, EngineConfigurationSettings>(); //All internal. No need for it to be called 
            container.RegisterTransient<IStateChanger, StateChanger>();
            container.RegisterSingleton<ICurrentStateAccessor, CurrentStateAccessor>();
        }
    }
}
