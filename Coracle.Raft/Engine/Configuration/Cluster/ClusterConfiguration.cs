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
using Coracle.Raft.Engine.ActivityLogger;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    public class ActivityConstants
    {
        #region Constants

        public const string Entity = nameof(ClusterConfiguration);
        public const string NewUpdate = nameof(NewUpdate);
        public const string CurrentNodeNotPartOfCluster = nameof(CurrentNodeNotPartOfCluster);
        public const string allNodeIds = nameof(allNodeIds);

        #endregion
    }

    internal sealed class ClusterConfiguration : IClusterConfiguration
    {
        object _lock = new object();

        public ClusterConfiguration(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
        }

        public ConcurrentDictionary<string, INodeConfiguration> PeerMap { get; private set; }

        public INodeConfiguration ThisNode { get; private set; }

        public bool IsThisNodePartOfCluster => ThisNode != null;

        public IEnumerable<INodeConfiguration> CurrentConfiguration
        {
            get
            {
                if (PeerMap == null)
                    return Enumerable.Empty<INodeConfiguration>();

                return IsThisNodePartOfCluster ? PeerMap.Values.Append(ThisNode) : PeerMap.Values;
            }
        }

        public IActivityLogger ActivityLogger { get; }

        public IEnumerable<INodeConfiguration> Peers
        {
            get
            {
                if (PeerMap != null && PeerMap.Count > 0)
                {
                    return PeerMap.Values;
                }

                return Enumerable.Empty<INodeConfiguration>();
            }
        }

        public INodeConfiguration GetPeerNodeConfiguration(string nodeId)
        {
            return PeerMap.TryGetValue(nodeId, out var node) ? node : null;
        }

        public void UpdateConfiguration(string thisNodeId, IEnumerable<INodeConfiguration> clusterConfiguration)
        {
            var map = new ConcurrentDictionary<string, INodeConfiguration>();
            INodeConfiguration thisNode = null;

            foreach (var node in clusterConfiguration)
            {
                map.TryAdd(node.UniqueNodeId, node);
            }

            bool isThisNodePartOfTheCluster = map.ContainsKey(thisNodeId);

            if (isThisNodePartOfTheCluster)
            {
                map.TryRemove(thisNodeId, out thisNode);
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActivityConstants.Entity,
                    Event = ActivityConstants.CurrentNodeNotPartOfCluster,
                    Level = ActivityLogLevel.Debug
                }
                .WithCallerInfo());
            }

            lock (_lock)
            {
                PeerMap = map;
                ThisNode = thisNode;
            }

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = ActivityConstants.Entity,
                Event = ActivityConstants.NewUpdate,
                Level = ActivityLogLevel.Debug
            }
            .With(ActivityParam.New(ActivityConstants.allNodeIds, CurrentConfiguration.Select(_ => _.UniqueNodeId).ToArray()))
            .WithCallerInfo());
        }
    }
}
