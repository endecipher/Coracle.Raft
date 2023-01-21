using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Registries;
using Coracle.Raft.Dependencies;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Remoting.RPC;
using EntityMonitoring.FluentAssertions.Structure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TaskGuidance.BackgroundProcessing.Dependencies;

namespace Coracle.IntegrationTests.Framework
{
    public interface INodeContext
    {
        void CreateMockNode(string nodeId);

        MockNodeContext GetMockNode(string nodeId);

        ConcurrentDictionary<string, object> KeyValues { get; }
    }

    public class MockNodeContext 
    {
        internal NodeConfiguration Configuration { get; init; }
        internal RemoteAwaitedLock<IAppendEntriesRPC, AppendEntriesRPCResponse> AppendEntriesLock { get; init; }
        internal RemoteAwaitedLock<IRequestVoteRPC, RequestVoteRPCResponse> RequestVoteLock { get; init; }

        public void EnqueueNextRequestVoteResponse(Func<IRequestVoteRPC, object> func, bool approveImmediately = false)
        {
            var @lock = RequestVoteLock.Enqueue(func);

            if (approveImmediately)
                @lock.Set();
        }

        public void ApproveNextRequestVoteInLine() => RequestVoteLock.ApproveNextInLine();

        public void EnqueueNextAppendEntriesResponse(Func<IAppendEntriesRPC, object> func, bool approveImmediately = false)
        {
            var @lock = AppendEntriesLock.Enqueue(func);

            if (approveImmediately)
                @lock.Set();
        }

        public void ApproveNextAppendEntriesInLine() => AppendEntriesLock.ApproveNextInLine();
    }


    public class NodeContext : INodeContext
    {
        ConcurrentDictionary<string, MockNodeContext> MockNodes { get; set; } = new ConcurrentDictionary<string, MockNodeContext>();

        public void CreateMockNode(string nodeId)
        {
            MockNodes.AddOrUpdate(nodeId, new MockNodeContext
            {
                Configuration = new NodeConfiguration
                {
                    UniqueNodeId = nodeId,
                },
                AppendEntriesLock = new RemoteAwaitedLock<IAppendEntriesRPC, AppendEntriesRPCResponse>(),
                RequestVoteLock = new RemoteAwaitedLock<IRequestVoteRPC, RequestVoteRPCResponse>(),
            },
            (key, oldConfig) => oldConfig);
        }

        public MockNodeContext GetMockNode(string nodeId)
        {
            bool isFound = MockNodes.TryGetValue(nodeId, out var node);

            if (isFound) { return node; } else throw new InvalidOperationException();
        }

        public ConcurrentDictionary<string, object> KeyValues { get; } = new ConcurrentDictionary<string, object>();
    }

    public class TestContext : IDisposable
    {
        public INodeContext NodeContext => ComponentContainer.Provider.GetRequiredService<INodeContext>();
        public ICommandContext CommandContext => ComponentContainer.Provider.GetRequiredService<ICommandContext>();

        public TestContext()
        {
            ComponentContainer.Configure();

            ComponentContainer.Remove<IElectionTimer>();
            ComponentContainer.Remove<IHeartbeatTimer>();
            ComponentContainer.ServiceDescriptors.AddSingleton<IElectionTimer, TestElectionTimer>();
            ComponentContainer.ServiceDescriptors.AddSingleton<IHeartbeatTimer, TestHeartbeatTimer>();

            ComponentContainer.ServiceDescriptors.AddSingleton<INodeContext, NodeContext>();
            ComponentContainer.ServiceDescriptors.AddSingleton<ICommandContext, CommandContext>();
            ComponentContainer.ServiceDescriptors.AddSingleton<IActivityMonitorSettings, ActivityMonitorSettings>();
            ComponentContainer.ServiceDescriptors.AddSingleton<IActivityMonitor<Activity>, ActivityMonitor<Activity>>();

            //Final step
            ComponentContainer.Build();
        }

        public T GetService<T>() => ComponentContainer.Provider.GetRequiredService<T>();

        

        public void Dispose()
        {

        }
    }


    internal static class ComponentContainer
    {
        public static ServiceProvider Provider { get; set; }
        public static IServiceCollection ServiceDescriptors { get; set; }

        public static void Configure()
        {
            var serviceCollection = new ServiceCollection()
                .AddLogging(l => l.AddConsole())
                .AddOptions()
                .Configure<LoggerFilterOptions>(c => c.MinLevel = LogLevel.Trace);

            var dotNetContainer = new DotNetDependencyContainer(serviceCollection);

            new CoracleDependencyRegistration().Register(dotNetContainer);
            new GuidanceDependencyRegistration().Register(dotNetContainer);
            new TestDependencyRegistration().Register(dotNetContainer);

            ServiceDescriptors = serviceCollection;
        }

        public static void Build()
        {
            Provider = ServiceDescriptors.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
        }

        public static void Remove<T>()
        {
            var descriptor = ServiceDescriptors.SingleOrDefault(d => d.ServiceType == typeof(T));

            if (descriptor != null)
                ServiceDescriptors.Remove(descriptor);
        }
    }

    public interface ICommandContext
    {
        int NextCommandCounter { get; }

        int GetLastCommandCounter { get; }
    }

    public class CommandContext : ICommandContext
    {
        private int _commandCounter = 0;
        private int _lastCommandCounter = 0;

        public int NextCommandCounter
        {
            get
            {
                _commandCounter++;
                _lastCommandCounter = _commandCounter;
                return _commandCounter;
            }
        }

        public int GetLastCommandCounter
        {
            get
            {
                return _lastCommandCounter;
            }
        }
    }
}
