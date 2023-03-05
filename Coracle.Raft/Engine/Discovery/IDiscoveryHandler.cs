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
using Coracle.Raft.Engine.Node;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Discovery
{
    /// <summary>
    /// <see cref="IDiscoveryHandler"/> holds the methods, used by the <see cref="CoracleNode"/> internally, to get the list of <see cref="INodeConfiguration"/>, 
    /// participating in the cluster. 
    /// 
    /// The storage and retrieval of all up nodes could be maintained via configuration file based systems (or) a registry server.
    /// 
    /// The methods used, are called only when <see cref="ICoracleNode.IsStarted"/> is <c>false</c> i.e. during initialization.
    /// </summary>
    public interface IDiscoveryHandler
    {
        /// <summary>
        /// Enrolls current node for Discovery. 
        /// For File-based configurations, it may not be necessary.
        /// </summary>
        /// <returns>A wrapper <see cref="DiscoveryResult"/> detailing over the discovery operation</returns>
        Task<DiscoveryResult> Enroll(INodeConfiguration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches all node information from the File-based system (or) the Registry Server (if Service Discovery is implemented). 
        /// </summary>
        /// <returns>
        /// A wrapper <see cref="DiscoveryResult"/> detailing over the discovery operation. 
        /// The <see cref="DiscoveryResult.AllNodes"/> would contain information about the operational nodes of the cluster post successful discovery
        /// </returns>
        Task<DiscoveryResult> GetAllNodes(CancellationToken cancellationToken);
    }
}
