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

using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Raft.Web.Controllers;
using System.Collections.Concurrent;

namespace Raft.Web.Canoe.Configuration
{
    public sealed class ClusterConfigurationSettings : IClusterConfiguration
    {
        public ClusterConfigurationSettings()
        {
            Peers = new ConcurrentDictionary<string, INodeConfiguration>();
        }

        public ConcurrentDictionary<string, INodeConfiguration> Peers { get; }

        public INodeConfiguration ThisNode { get; } 

        public void AddPeer(INodeConfiguration nodeConfiguration)
        {
            Peers.TryAdd(nodeConfiguration.UniqueNodeId, nodeConfiguration);
        }

        public INodeConfiguration GenerateRandomNode()
        {
            return new NodeConfiguration
            {
                UniqueNodeId = $"_Node{Peers.Count + 1}_",
            };
        }
    }

    public sealed class NodeConfiguration : INodeConfiguration
    {
        public string UniqueNodeId { get; init; }

        public Uri BaseUri { get; set; }

        public string AppendEntriesEndpoint { get; set; } = Routes.Endpoints.AppendEntries;

        public string RequestVoteEndpoint { get; set; } = Routes.Endpoints.RequestVote;

        public string CommandHandlingEndpoint { get; set; } = Routes.Endpoints.HandleClientCommand;
    }
}
