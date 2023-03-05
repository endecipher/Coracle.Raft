using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Tests.Components.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace Coracle.Raft.Tests.Framework
{
    public class TestContext
    {
        public IMockNodeContext NodeContext => ServiceContainer.Provider.GetRequiredService<IMockNodeContext>();
        public ICommandCounter CommandContext => ServiceContainer.Provider.GetRequiredService<ICommandCounter>();

        public TestContext()
        {
            ServiceContainer.Configure();

            ServiceContainer.Remove<IElectionTimer>();
            ServiceContainer.Remove<IHeartbeatTimer>();

            ServiceContainer.ServiceDescriptors.AddSingleton<IElectionTimer, TestElectionTimer>();
            ServiceContainer.ServiceDescriptors.AddSingleton<IHeartbeatTimer, TestHeartbeatTimer>();

            ServiceContainer.ServiceDescriptors.AddSingleton<IMockNodeContext, MockNodeContext>();
            ServiceContainer.ServiceDescriptors.AddSingleton<ICommandCounter, CommandCounter>();

            ServiceContainer.Build();
        }

        public T GetService<T>() => ServiceContainer.Provider.GetRequiredService<T>();
    }
}
