﻿using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using System.Collections.Concurrent;

namespace Coracle.IntegrationTests.Components.Configuration
{
    public class SettingOptions
    {
        public ConcurrentDictionary<string, INodeConfiguration> Peers { get; set; }

        public INodeConfiguration ThisNode { get; set; }
    }
}
