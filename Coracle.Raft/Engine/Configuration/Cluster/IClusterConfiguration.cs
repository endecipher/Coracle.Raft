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

using System.Collections.Generic;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    /// <summary>
    /// In-Memory track of Node Configurations
    /// </summary>
    public interface IClusterConfiguration
    {
        /// <summary>
        /// Collection of Peer nodes which are part of the cluster
        /// </summary>
        IEnumerable<INodeConfiguration> Peers { get; }

        /// <summary>
        /// Gets current node configuration from the operational clsuter.
        /// If <see cref="ThisNode"/> is null, then we can assume, that the current node is not part of the Cluster Configuration.
        /// </summary>
        INodeConfiguration ThisNode { get; }

        /// <summary>
        /// Checks if <see cref="ThisNode"/> is not null, internally
        /// </summary>
        bool IsThisNodePartOfCluster { get; }

        /// <summary>
        /// Updates the core configuration
        /// </summary>
        /// <param name="thisNodeId">Current Node Id</param>
        /// <param name="allNodes">All Nodes' info in the cluster configuration</param>
        void UpdateConfiguration(string thisNodeId, IEnumerable<INodeConfiguration> allNodes);

        /// <summary>
        /// Gets Current List of Node Configurations
        /// </summary>
        IEnumerable<INodeConfiguration> CurrentConfiguration { get; }

        /// <summary>
        /// Get the <see cref="INodeConfiguration"/> for a given node (peer). 
        /// Returns null, if not found
        /// </summary>
        /// <param name="nodeId">External Server Id</param>
        INodeConfiguration GetPeerNodeConfiguration(string nodeId);
    }

    /// <remarks>
    /// For the configuration change mechanism to be safe, there must be no point during the transition where it
    /// is possible for two leaders to be elected for the same term. Unfortunately, any approach where servers switch
    /// directly from the old configuration to the new configuration is unsafe. It isn’t possible to atomically switch all of
    /// the servers at once, so the cluster can potentially split into two independent majorities during the transition
    /// <seealso cref="Section 6 Cluster membership changes"/>
    /// </remarks>
}
