#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Remoting;
using TaskGuidance.BackgroundProcessing.Dependencies;
using Coracle.Raft.Engine.Command;
using EntityMonitoring.FluentAssertions.Structure;
using Coracle.Raft.Examples.Registrar;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Examples.Data;
using Coracle.Raft.Tests.Components.Discovery;
using Coracle.Raft.Tests.Components.Logging;
using Coracle.Raft.Tests.Components.Remoting;

namespace Coracle.Raft.Tests.Framework
{
    public class TestRegistration : IDependencyRegistration
    {
        public void Register(IDependencyContainer container)
        {
            container.RegisterSingleton<IStateMachineHandler, NoteKeeperStateMachineHandler>();
            container.RegisterSingleton<IPersistentStateHandler, SampleVolatileStateHandler>();
            container.RegisterSingleton<IDiscoveryHandler, TestDiscoveryHandler>();
            container.RegisterSingleton<IOutboundRequestHandler, TestOutboundRequestHandler>();

            container.RegisterSingleton<INoteStorage, NoteStorage>();
            container.RegisterSingleton<IActivityLogger, TestActivityLogger>();
            container.RegisterSingleton<INodeRegistrar, TestNodeRegistrar>();
            container.RegisterSingleton<INodeRegistry, TestNodeRegistry>();
            container.RegisterSingleton<ISnapshotManager, SnapshotManager>();

            /// EntityMonitoring.FluentAssertions
            container.RegisterSingleton<IActivityMonitorSettings, ActivityMonitorSettings>();
            container.RegisterSingleton<IActivityMonitor<Activity>, ActivityMonitor<Activity>>();
        }
    }
}
