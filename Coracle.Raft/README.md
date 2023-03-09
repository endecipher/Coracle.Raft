#  Coracle.Raft

[![NuGet version (Coracle.Raft)](https://img.shields.io/nuget/v/Coracle.Raft.svg?style=flat-square)](https://www.nuget.org/packages/Coracle.Raft/)![GitHub](https://img.shields.io/github/license/endecipher/Coracle.Raft?color=sienna)[![Dependency](https://badgen.net/badge/uses/TaskGuidance.BackgroundProcessing/purple?icon=nuget)](https://www.nuget.org/packages/TaskGuidance.BackgroundProcessing)

## Documentation and RAFT Processing

The entire functionality of `Coracle.Raft` (leader election, log replication, safety, command-handling, snapshots, configuration changes) is split across different dependencies and internal actions.

For each Application Domain, a single `Coracle` node can exist. 

### Starting a `Coracle.Raft` node

To initialize the `Coracle` node, the `ICoracleNode.InitializeConfiguration()` is called. 

During initialization, internally the following occurs:  
- The `IEngineConfiguration` settings is validated. The `IEngineConfiguration` must be proper since internal validations may be thrown.
- The **`IDiscoveryHandler`** is invoked to gather information about other nodes in the cluster. This is the only-time when **`IDiscoveryHandler`** is used. In a 2-step process, the node first calls `Enroll()` to enroll itself, and then calls `GetAllNodes()` to obtain info about other nodes in the cluster. 
    - The discovery of other nodes may be static via file-based configurations, or dynamic using a Discovery server when the node starts. 

Once intialization is done, then `ICoracleNode.Start()` may be called to start internal processing. 
`IResponsibilities` from [TaskGuidance.BackgroundProcessing](https://www.nuget.org/packages/TaskGuidance.BackgroundProcessing) is initialized for all internal background action processing.

### Leader Establishment
Once the node starts, as RAFT suggests, the initial state is `Follower`. The number of functional nodes in cluster must be equal to or more than 3, and for majorities to follow, an odd number is suggested for achieving quorum. The node would then time-out and become a `Candidate` and start elections; and internally fire RequestVoteRPCs to other nodes. 

The specifics of how the node serializes the data and communicates with other coracle nodes is all left to the implementation of **`IOutboundRequestHandler`**. 

The node throughout its lifetime will receive multiple RPCs (e.g. the node might receive RequestVoteRPCs from other nodes which initially could undergo a similar timeout simultaneously), and thus, the deserialization of the data received at different endpoints into the corresponding RPC objects must also be implemented by the user of this library. 
- Once the RPC object is obtained, `IRemoteCallExecutor.RespondTo()` can be invoked by passing the RPC object and getting back the result which needs to be sent back to the node which sent the RPC originally.
- `RespondTo()` can handle AppendEntriesRPC, RequestVoteRPC and InstallSnapshotRPC

Assuming the node in context receives a majority of votes, the node changes its own state to `Leader`. Once `Leader`, it would write a new entry to its log and send multiple outbound AppendEntriesRPC to other nodes. This RPC is not only used to replicate logEntries, but also serves as a heartbeat.

### Commands
The functionality of the cluster can be described by the way `ICommand` works and how the **`IStateMachineHandler`** interprets these commands. 
- The purpose of the cluster implementing `Coracle.Raft` can be anything; it could be to maintain a replicated state machine which represents a distributed key-value store, or something much more complex. However, the way things work remains the same internally.
- The `ICommand` contains some data which the **`IStateMachineHandler`** can work with.
- The `ICommand` contains some mandatory properties:
  - `string` UniqueId — Unique Command Id
  - `bool` IsReadOnly — Whether the command is a Read-Only command or not
  - `string` Type — Type of command (Value dictated by use-case)
- `ICommand` may be implemented by some class to represent 

A Coracle **Client** may want to interact with the cluster. It may want to perform multiple CRUD operations over the state. Regardless, the `ICommand` must reflect the intention of the operation, and the implementation of **`IStateMachineHandler`** (which exposes method for read and applying commands) must be compatible with the `ICommand` format.
- For issuing commands, the internal component `ICommandExecutor` can be called from DI, and `ICommandExecutor.Execute()` can be invoked with the `ICommand` object.
- For non-readonly commands:
    - Since non-readonly commands are handled by the `Leader`, internally, the node would write a new entry into its log, and make sure that the command logEntry is replicated across a majority of the cluster. Once that occurs, the `Leader` would commit that entry, and apply it to the state machine using **`IStateMachineHandler.ExecuteAndApply()`**.  
    - Once the command is applied, the `Execute()` call ends, as the result for the applied command is returned using **`IStateMachineHandler.TryGetResult()`**
- For read-only commands:
    - Since read-only commands need linearizable outputs, the `Leader` again has to repond to the command. 
    - The `Leader` has to make sure it has not deposed (i.e check if it is still the leader of the cluster) before it responds to the command.
    - Thus, it would wait and affirm via background AppendEntriesRPCs, and finally respond back using **`IStateMachineHandler.TryGetResult()`**.
     
### Change Cluster Membership
If there is a need to add/remove nodes from the operational cluster, RAFT suggests to use the "Joint-Consensus" approach.
The above workflow is internally handled, and the `IConfigurationRequestExecutor.IssueChange()` can be called by supplying the desired configuration.

### Snapshot Installation
If a node is added to the cluster, it may take a long time via the traditional AppendEntriesRPC approach to catch-up, since transmitting huge amount of entries over the network might be resource and time consuming. 
For this reason, RAFT suggests the usage of InstallSnapshotRPC. In the background, a node compacts logEntries based on certain criteria, and the state of the system is chunked and stored under a snapshot. The `Leader` would then send multiple InstallSnapshotRPC containing chunks of data, which would be received by the new node. Once all chunks are received, the new node can call **`IStateMachineHandler.ForceRebuildFromSnapshot()`** to make sure that the state is in sync upto the last compacted logEntry.

### Persistent data handling
During all of the above, it is important to mention that a lot of involatile data is accessed and manipulated via internal actions.  
1. *CurrentTerm* manipulations - To keep track of/update the current term number    
2. *VotedFor* manipulations - To keep track of/update the nodeId for whom the node voted for
3. *LogEntries* management - To maintain the logEntry chain, and fetch entries
4. *Snapshot* management - To maintain snapshots and compaction processing

For the above, `IPersistenceHandler` exposes different methods which can be implemented by the user of this library.



## Setup
### Dependency Injection
For DI Registration, wrap your chosen DI Container under `TaskGuidance.BackgroundProcessing.Dependencies.IDependencyContainer` and call `Coracle.Raft.Dependencies.Registration.Register(IDependencyContainer)` to implicitly register all dependencies during application startup.

```C# 
public class DependencyContainer : IDependencyContainer
{
    /* dotNet DI example. Autofac or other DI containers can be used */
    IServiceCollection ServiceDescriptors { get; set; } 
    
    void IDependencyContainer.RegisterSingleton<T1, T2>()
    {
        ServiceDescriptors.AddSingleton<T1, T2>();
    }
    
    void IDependencyContainer.RegisterTransient<T1, T2>()
    {
        ServiceDescriptors.AddTransient<T1, T2>();
    }
}
```

### Configuration Settings

The `Coracle.Raft.Engine.Node.IEngineConfiguration` would internally be used as a singleton to get the static configurations.

Since a `Coracle` node may not only send RPCs, but also repond to received RPCs, the time-outs for each remote call processing has been split, for finer control.

```
"EngineConfiguration": {
      "AppendEntriesTimeoutOnReceive_InMilliseconds": 100, 
      "AppendEntriesTimeoutOnSend_InMilliseconds": 100,
      "CatchUpOfNewNodesTimeout_InMilliseconds": 60000, 
      "CatchUpOfNewNodesWaitInterval_InMilliseconds": 2,
      "CheckDepositionWaitInterval_InMilliseconds": 2,
      "ClientCommandTimeout_InMilliseconds": 5000,
      "ConfigurationChangeHandleTimeout_InMilliseconds": 30000,
      "EntryCommitWaitInterval_InMilliseconds": 2,
      "EntryCommitWaitTimeout_InMilliseconds": 200,
      "HeartbeatInterval_InMilliseconds": 2,
      "IncludeConfigurationChangeRequestInResults": true,
      "IncludeJointConsensusConfigurationInResults": true,
      "IncludeOriginalClientCommandInResults": true,
      "IncludeOriginalConfigurationInResults": true,
      "MaxElectionTimeout_InMilliseconds": 500,
      "MinElectionTimeout_InMilliseconds": 250,
      "NodeId": "NodeDebug",
      "NodeUri": null,
      "NoLeaderElectedWaitInterval_InMilliseconds": 2,
      "ProcessorQueueSize": 100,
      "ProcessorWaitTimeWhenQueueEmpty_InMilliseconds": 1,
      "RequestVoteTimeoutOnReceive_InMilliseconds": 100,
      "RequestVoteTimeoutOnSend_InMilliseconds": 100,
      "WaitPostEnroll_InMilliseconds": 5000,
      "InstallSnapshotChunkTimeoutOnSend_InMilliseconds": 10000,
      "InstallSnapshotChunkTimeoutOnReceive_InMilliseconds": 10000,
      "CompactionAttemptTimeout_InMilliseconds": 600000,
      "CompactionAttemptInterval_InMilliseconds": 1000,
      "CompactionWaitPeriod_InMilliseconds": 5000,
      "SnapshotThresholdSize" : 5,
      "SnapshotBufferSizeFromLastEntry" :  1
    }
```

- `NodeId` is a string which must be unique for every application node.
- `NodeUri` is the uri/address of the application node. If statically not known, it can be populated dynamically during application runtime. 
- A general rule of thumb is to supply the Time settings which accruately potray the below:

  `ProcessorWaitTimeWhenQueueEmpty << CheckDepositionWaitInterval <= EntryCommitWaitInterval <= CatchUpOfNewNodesWaitInterval <= HeartbeatInterval <= NoLeaderElectedWaitInterval << (AppendEntriesTimeoutOnReceive, AppendEntriesTimeoutOnSend, RequestVoteTimeoutOnReceive, RequestVoteTimeoutOnSend) < EntryCommitWaitTimeout < MinElectionTimeout < MaxElectionTimeout < ClientCommandTimeout < CatchUpOfNewNodesTimeout <= ConfigurationChangeHandleTimeout << MTBF (Mean Time Between Failures)`

- `CheckDepositionWaitInterval` controls the aggression of checking whether the leader node has been deposed or not, since it's required for responding to Read-only requests.
- `EntryCommitWaitInterval` and `EntryCommitWaitTimeout` denotes the processing timeout and interval respectively, for checking whether a logEntry has been replicated safely to a majority of the cluster.
- `NoLeaderElectedWaitInterval` determines how aggressively the application node should check whether a Leader Node is elected for the cluster, when there isn't one.
- `WaitPostEnroll` is only used initially, i.e at the start of the node, and the system waits for the specified time for the discovery of other nodes, if a Discovery Server is used. For File-based configurations, the value maybe specified as 0. 
- `InstallSnapshotChunkTimeoutOnSend` and `InstallSnapshotChunkTimeoutOnReceive` are for action processing timouts during InstallSnapshotRPC. Since a large amount of data resembling the state might be transferred, it can be set per use-case. It has to be lesser than the `CatchUpOfNewNodesTimeout`, since snapshots come into play when a new node is added to the cluster.
- `CompactionAttemptTimeout` denotes the processing timeout to create a snapshot, if eligible.
- `CompactionAttemptInterval` specifies an interval to wait before the next retry of the compaction process, if eligibility fails.
- `CompactionWaitPeriod` contributes to the eligibility of the compaction process, since it denotes the amount of time to wait since the Current Snapshot is used.
- `SnapshotThresholdSize` denotes the size of entries to compact. 
- `SnapshotBufferSizeFromLastEntry` denotes a buffer size required from the last applied entry.
- Since the nuget package [TaskGuidance.BackgroundProcessing](https://www.nuget.org/packages/TaskGuidance.BackgroundProcessing) is used internally, `ProcessorQueueSize` and `ProcessorWaitTimeWhenQueueEmpty` can be specified accordingly as well.

## Necessary `*Handler` Extensions to implement

- `Coracle.Raft.Engine.Remoting.IOutboundRequestHandler` 
    - Needs to be implemented for Outbound Remote Operations.
    - No restrictions on protocols. (HTTP APIs/gRPC with Protobuf serialization etc)
    - This dependency is internally called, whenever an outbound RPC needs to be sent.
    

- `Coracle.Raft.Engine.Discovery.IDiscoveryHandler`
    - Needs to be implemented for initial discovery of other `Coracle` nodes.
    - During node initialization, a call is made to enroll, and after `WaitPostEnroll` period, another call is made to get the details of other nodes.
    - The above is helpful for Discovery servers; but for static configurations (like file-based), enrolling can be avoided.
    - The job of `IDiscoveryHandler` is done once the node successfully starts.


- `Coracle.Raft.Engine.Command.IStateMachineHandler`
  - Needs to be implemented for maintaining the State Machine, i.e the cluster's core functionality.
  - Since the state is built using the Commands initiated from clients, the `Coracle.Raft.Engine.Command.ICommand` can be inherited to model ANY use-case.
  - The `IStateMachine` will apply non-readonly commands when instructed to, internally.
  - It will also store the latest commands and their execution results, and will retrieve the results of read-only commands.
  - On rare ocassions, the entire state would need to be re-built from an externally received snapshot, and the `IStateMachineHandler` handles that as well.
  

- `Coracle.Raft.Engine.States.IPersistentStateHandler`
  -  Needs to be implemented for managing persistent properties like *CurrentTerm* and *VotedFor*, state Snapshot operations, and the replicated log using any data storage technology.
  - The replicated log stores multiple types of logEntries, and `IPersistentStateHandler` also defines basic operations to implement which surround these types. More can be found from `Coracle.Raft.Engine.Logs.LogEntry.Types`:
    - None (No contents; used as the first entry of the log chain)
    - NoOperation (Appended during leader establishment)
    - Command (Appended when client commands are encountered)
    - Configuration (Appended when configuration change is requested)
    - Snapshot (Corresponding entry which points to a snapshot containing subset of the state)
   
### Interacting with `Coracle`'s internally defined components

To interact with the started `Coracle` node, the following may be called via DI:

- `Coracle.Raft.Engine.Command.ICommandExecutor` 
    - Since the state is built using the Commands initiated from clients, the `Coracle.Raft.Engine.Command.ICommand` can be inherited to model ANY use-case. The `ICommand` contains:
        - `string` UniqueId --- Unique Command Id
        - `bool` IsReadOnly --- Whether the command is a Read-Only command or not
        - `string` Type --- Type of command (Value dictated by use-case)
    - The implementation of ICommand can contain more data and properties, however for internal processing, the above 3 are mandatory.
    - This dependency can be externally called, whenever there is a need to execute a command requested by the client. 
    ``` C#
    Task<CommandExecutionResult> ICommandExecutor.Execute<TCommand>(TCommand Command, CancellationToken cancellationToken)
            where TCommand : class, ICommand
    ```

- `Coracle.Raft.Engine.Remoting.IRemoteCallExecutor`
    - This `Coracle` node might be contacted by other `Coracle` nodes for AppendEntries/RequestVote or InstallSnapshot RPCs.
    - To process such requests, the dependency `IRemoteCallExecutor` can be externally called.
    - `IRemoteCallExecutor.RespondTo()` can be invoked by passing the deserialized RPC, and the appropriate CancellationToken.
    - The output of the above can then be utilized to respond back to the other node which originally sent the RPC.


- `Coracle.Raft.Engine.Configuration.Alterations.IConfigurationRequestExecutor`
  - Post the initial discovery, if there is a need for the operating cluster of nodes to change, then the `IConfigurationRequestExecutor.IssueChange()` should be invoked, by passing the desired cluster configuration.
  - Since this request can only be processed by the leader of the cluster, the leader node internally calculates the Join-Consensus entry and waits for a majority of the nodes to replicate this Configuration Log Entry.
  - Once done, the leader appends another configuration logEntry denoting the target/desired configuration.
  - The nodes which are marked for exclusion in the issued request (which may include the leader) decommission themselves, and remain inactive.
  

- `Coracle.Raft.Engine.Node.ICoracleNode`
  -  The `ICoracleNode.InitializeConfiguration()` should be invoked once the `IEngineConfiguration` is successfully pointing to the Node config, and the enrolment of the current node and discovery of other nodes can proceed.
     - The `IEngineValidator` is called to check if the `IEngineConfiguration` is valid or not. An exception is thrown if there are any issues. 
  - The `ICoracleNode.Start()` can be invoked once the `InitializeConfiguration()` is successfully completed and the node is aware of other nodes.
    - This operation internally sets the state of the node to `Follower`
    - Configures a new set of `IResponsibilities` for action processing


## Example Usage 

Check out the sample implementation [Coracle.Web.Examples](https://github.com/endecipher/Coracle.Web.Examples) which 
- Implements `Coracle.Raft.Engine.Remoting.IOutboundRequestHandler` and uses HTTP-based APIs and JSON payloads to communicate with other nodes
- Implements `Coracle.Raft.Engine.Discovery.IDiscoveryHandler` and uses a discovery server
- Implements `Coracle.Raft.Engine.Command.IStateMachineHandler` and maintains a state machine and accepts corresponding commands which keep track of notes/key-value string pairs
- Implements `Coracle.Raft.Engine.States.IPersistentStateHandler` for data-handling operations. For understanding purposes and keeping it simple, all operations were implemented in-memory instead of using a persistent data store. 