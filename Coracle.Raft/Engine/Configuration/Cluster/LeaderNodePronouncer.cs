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
using Coracle.Raft.Engine.Discovery;

namespace Coracle.Raft.Engine.Configuration.Cluster
{
    internal sealed class LeaderNodePronouncer : ILeaderNodePronouncer
    {
        #region Constants

        public const string CannotRecognizeAsNotInCluster = nameof(CannotRecognizeAsNotInCluster);
        public const string CannotRecognizeItselfAsNotInCluster = nameof(CannotRecognizeItselfAsNotInCluster);
        public const string LeaderRecognizedAsItself = nameof(LeaderRecognizedAsItself);
        public const string LeaderRecognized = nameof(LeaderRecognized);
        public const string Entity = nameof(LeaderNodePronouncer);
        public const string nodeIdToUpdate = nameof(nodeIdToUpdate);

        #endregion

        public LeaderNodePronouncer(IClusterConfiguration clusterConfiguration, IDiscoveryHandler discoverer, IActivityLogger activityLogger)
        {
            ClusterConfiguration = clusterConfiguration;
            Discoverer = discoverer;
            ActivityLogger = activityLogger;
        }

        public INodeConfiguration RecognizedLeaderConfiguration { get; private set; } = null;
        public IClusterConfiguration ClusterConfiguration { get; }
        public IDiscoveryHandler Discoverer { get; }
        public IActivityLogger ActivityLogger { get; }

        public void SetNewLeader(string leaderServerId)
        {
            var configuration = ClusterConfiguration.GetPeerNodeConfiguration(leaderServerId);

            if (configuration != null)
            {
                RecognizedLeaderConfiguration = configuration;

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = LeaderRecognized,
                    Level = ActivityLogLevel.Information,
                }
                .With(ActivityParam.New(nodeIdToUpdate, leaderServerId))
                .WithCallerInfo());

                return;
            }
            else
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = CannotRecognizeAsNotInCluster,
                    Level = ActivityLogLevel.Error,
                }
                .With(ActivityParam.New(nodeIdToUpdate, leaderServerId))
                .WithCallerInfo());
            }
        }

        public void SetRunningNodeAsLeader()
        {
            if (!ClusterConfiguration.IsThisNodePartOfCluster)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = Entity,
                    Event = CannotRecognizeItselfAsNotInCluster,
                    Level = ActivityLogLevel.Error,
                }
                .WithCallerInfo());
            }

            RecognizedLeaderConfiguration = ClusterConfiguration.ThisNode;

            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = LeaderRecognizedAsItself,
                Level = ActivityLogLevel.Information,
            }
            .WithCallerInfo());
        }
    }
}
