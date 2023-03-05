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

using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Examples.Registrar;
using System.Collections.Concurrent;

namespace Coracle.Raft.Tests.Components.Discovery
{
    public class TestNodeRegistry : INodeRegistry
    {
        ConcurrentDictionary<string, NodeConfiguration> MemoryRegistry = new ConcurrentDictionary<string, NodeConfiguration>();

        public Task AddOrUpdate(NodeConfiguration configuration)
        {
            MemoryRegistry.AddOrUpdate(configuration.UniqueNodeId, configuration, (key, conf) => configuration);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<NodeConfiguration>> GetAll()
        {
            return Task.FromResult(MemoryRegistry.Values.AsEnumerable());
        }

        public Task TryRemove(string nodeId)
        {
            MemoryRegistry.TryRemove(nodeId, out var configuration);

            return Task.CompletedTask;
        }
    }
}
