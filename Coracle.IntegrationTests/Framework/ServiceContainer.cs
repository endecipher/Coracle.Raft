using Coracle.Raft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskGuidance.BackgroundProcessing.Dependencies;

namespace Coracle.IntegrationTests.Framework
{
    internal static class ServiceContainer
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

            /// TaskGuidance.BackgroundProcessing Registration
            new GuidanceDependencyRegistration().Register(dotNetContainer);

            /// Coracle.Raft Registration
            new Registration().Register(dotNetContainer);

            /// Test Extensions Registration
            new TestRegistration().Register(dotNetContainer);

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
}
