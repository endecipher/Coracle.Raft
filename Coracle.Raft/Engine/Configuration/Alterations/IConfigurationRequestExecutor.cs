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

using Coracle.Raft.Engine.Node;
using System.Threading;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Configuration.Alterations
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConfigurationRequestExecutor
    {
        /// <summary>
        /// Used by an external entity, to issue configuration changes post <see cref="ICoracleNode.InitializeConfiguration"/>.
        /// 
        /// The configuration change is issued when the <see cref="ICoracleNode.IsStarted"/> is <c>true</c>, 
        /// and the processing may occur when the current Node is an active <see cref="States.Leader"/> of the Cluster.
        /// </summary>
        /// <param name="configurationChangeRequest">Change Request which denotes the target configuration of the cluster</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<ConfigurationChangeResult> IssueChange(ConfigurationChangeRequest configurationChangeRequest, CancellationToken cancellationToken);
    }
}
