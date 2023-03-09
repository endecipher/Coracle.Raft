<img alt="packageIcon" src="https://github.com/endecipher/Coracle.Raft/blob/main/Coracle.Raft/packageIcon.png" width=20% height=20%> 

#  Coracle.Raft

*Clusterize* your functionality and build your own replicated state machine using this completely extensible implementation of the Raft consensus algorithm in .NET.  

[![NuGet version (Coracle.Raft)](https://img.shields.io/nuget/v/Coracle.Raft.svg?style=flat-square)](https://www.nuget.org/packages/Coracle.Raft/)![GitHub](https://img.shields.io/github/license/endecipher/Coracle.Raft?color=indigo)

[![Coracle.Raft - Project Build and Integration Testing](https://github.com/endecipher/Coracle.Raft/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/endecipher/Coracle.Raft/actions/workflows/build-and-test.yml)[![Tests](https://badgen.net/badge/tests/26/cyan?icon=github)](https://github.com/endecipher/Coracle.Raft/actions/workflows/build-and-test.yml/badge.svg?branch=main)[![Windows Build and Tests passing](https://badgen.net/badge/windows-latest/passing/green?icon=github)](https://github.com/endecipher/Coracle.Raft/actions/workflows/build-and-test.yml/badge.svg?branch=main)[![MacOS Build and Tests passing](https://badgen.net/badge/macos-latest/passing/green?icon=github)](https://github.com/endecipher/Coracle.Raft/actions/workflows/build-and-test.yml/badge.svg?branch=main)[![Ubuntu Build and Tests passing](https://badgen.net/badge/ubuntu-latest/passing/green?icon=github)](https://github.com/endecipher/Coracle.Raft/actions/workflows/build-and-test.yml/badge.svg?branch=main)[![Dependency](https://badgen.net/badge/uses/TaskGuidance.BackgroundProcessing/purple?icon=nuget)](https://www.nuget.org/packages/TaskGuidance.BackgroundProcessing)

## Features

- Control important functionality
    - Extend `Coracle.Raft.Engine.Remoting.IOutboundRequestHandler` for Outbound Remote Operations without any restrictions on protocols and data-transfer formats
    - Extend `Coracle.Raft.Engine.Discovery.IDiscoveryHandler` for initial discovery of other `Coracle` nodes
    - Extend `Coracle.Raft.Engine.Command.IStateMachineHandler` for  maintaining the State Machine, i.e your cluster's core state functionality
    - Extend `Coracle.Raft.Engine.States.IPersistentStateHandler` for managing crucial persistent properties, state snapshots, and the replicated log using any data storage technology
- Supports configuration changes using the cited "Joint-Consensus" approach 
- Supports quick catch-up of newly added nodes using InstallSnapshotRPC
- Fine-grained control over internal processing using `IEngineConfiguration` settings 
- Extensive and rich logging of all internal workflows  
    - Control log Level and integrate extensible Log sinks
    - For consistency, `IActivityLogger` can be used again for capturing logs from implementations of the aforementioned handlers
- Easy DI registration


## Documentation
- To know more about RAFT, check out [In Search of an Understandable Consensus Algorithm - Diego Ongaro and John Ousterhout, Stanford University](https://web.stanford.edu/~ouster/cgi-bin/papers/raft-atc14)
- To know more about how Coracle.Raft works and implements the algorithm, check out [Coracle.Raft](https://github.com/endecipher/Coracle.Raft/tree/main/Coracle.Raft)
