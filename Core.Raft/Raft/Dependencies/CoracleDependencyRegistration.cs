using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.Discovery.Registrar;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;
using TaskGuidance.BackgroundProcessing.Dependencies;

namespace Coracle.Raft.Dependencies
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
    /// <item><see cref = "IClusterConfiguration" /> - For seeing the cluster nodes identified and being communicated with. This stores the internal Configuration of the Cluster in memory. </item>
    /// <item><see cref = "ICanoeNode" /> - For initialization and system operations on the Coracle Node</item>
    /// <item><see cref = "IExternalRpcHandler" /> - For handling inbound RPC calls from other Coracle nodes</item>
    /// <item><see cref = "ICurrentStateAccessor" /> - For holding the Current State used by the entire application</item>
    /// <item><see cref = "IClusterConfigurationChanger" /> - For changing the internal <see cref="IClusterConfiguration"/> during <see cref="IExternalConfigurationChangeHandler.IssueConfigurationChange(ConfigurationChangeRPC, System.Threading.CancellationToken)"/></item>
    /// <item><see cref = "IAppendEntriesManager" /> - Initialized for centrally managing outbound AppendEntriesRPC during <see cref="Leader"/> state</item>
    /// <item><see cref = "IElectionManager" /> - For centrally managing outbound RequestVoteRPC during <see cref="Candidate"/> state</item>
    /// <item><see cref = "IExternalConfigurationChangeHandler" /> - For handling inbound RPC calls issued for configuration change</item>
    /// <item><see cref = "ILeaderVolatileProperties" /> - Initialized for handling NextIndex and MatchIndex during <see cref="Leader"/> state</item>
    /// </list>
    /// 
    /// </summary>
    public sealed class CoracleDependencyRegistration : IDependencyRegistration
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
            container.RegisterSingleton<IClusterConfigurationChanger, ClusterConfigurationChanger>();
            container.RegisterSingleton<IAppendEntriesManager, AppendEntriesManager>();
            container.RegisterSingleton<IElectionManager, ElectionManager>();
            container.RegisterTransient<IExternalConfigurationChangeHandler, ExternalConfigurationChangeHandler>();
            container.RegisterSingleton<ILeaderVolatileProperties, LeaderVolatileProperties>();
        }
    }
}
