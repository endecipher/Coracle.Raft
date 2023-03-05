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

using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Logs.CollectionStructure;
using Core.Raft.Canoe.Engine.Remoting;
using Core.Raft.Canoe.Engine.States;
using Core.Raft.Extensions.Canoe.Remoting;
using EventGuidance.Logging;
using Raft.Web.Canoe.ClientHandling;
using Raft.Web.Canoe.ClientHandling.Notes;
using Raft.Web.Canoe.Configuration;
using Raft.Web.Canoe.Logging;
using Raft.Web.Canoe.PersistentData;
using StructureMap;

namespace Raft.Web.Canoe.Registries
{
    public class ExtensionRegistry : Registry
    {
        public IHttpClientFactory Factory { get; }

        public ExtensionRegistry(IHttpClientFactory factory)
        {
            Factory = factory;
        }

        public ExtensionRegistry()
        {
            For<INotes>().Use<Notes>().Singleton();
            For<IRemoteManager>().Use<HttpRemoteManager>().Singleton();
            For<IClientRequestHandler>().Use<SimpleClientRequestHandler>().Singleton();
            For<IEventLogger>().Use<SimpleEventLogger>().Singleton();
            For<IPersistentProperties>().Use<MemoryProperties>().Singleton();
            For<IPersistentReplicatedLogHolder>().Use<MemoryLogHolder> ().Singleton();
            For<IEventProcessorConfiguration>().Use<EventProcessorConfiguration>().Singleton();
            For<IClusterConfiguration>().Use<ClusterConfigurationSettings>().Singleton();
            For<ITimeConfiguration>().Use<TimeConfiguration>().Singleton();
            For<IOtherConfiguration>().Use<OtherConfiguration>().Singleton();
        }
    }
}
