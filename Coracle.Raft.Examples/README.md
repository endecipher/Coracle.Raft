#  Coracle.Raft.Examples

An example use case of [Coracle.Raft](https://github.com/endecipher/Coracle.Raft/).
This package is used by the testing framework, and [Coracle.Web.Examples](https://github.com/endecipher/Coracle.Web.Examples/)

[![NuGet version (Coracle.Raft.Examples)](https://img.shields.io/nuget/v/Coracle.Raft.Examples.svg)](https://www.nuget.org/packages/Coracle.Raft.Examples/)![GitHub](https://img.shields.io/github/license/endecipher/Coracle.Raft?color=indigo)

## Features

- `Coracle.Raft.Engine.Command.ICommand` and `Coracle.Raft.Engine.Command.IStateMachineHandler` are implemented for keeping track of "notes" or any string key-value pairs
- Namespace `Coracle.Raft.Examples.Registrar` exposes interfaces for service discovery and storage of enrolled nodes
- `Coracle.Raft.Engine.Command.IStateMachineHandler` is implemented for all data operations. For understanding purposes, an in-memory extension was adopted. All expected operations are covered - inclusive of snapshot management, compaction, handling the replicated log chain etc
