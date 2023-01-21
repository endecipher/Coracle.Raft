using Microsoft.Extensions.DependencyInjection;
using TaskGuidance.BackgroundProcessing.Dependencies;

namespace Coracle.IntegrationTests.Framework
{
    public class DotNetDependencyContainer : IDependencyContainer
    {
        public DotNetDependencyContainer(IServiceCollection serviceDescriptors)
        {
            ServiceDescriptors = serviceDescriptors;
        }

        public IServiceCollection ServiceDescriptors { get; }

        void IDependencyContainer.RegisterSingleton<T1, T2>()
        {
            ServiceDescriptors.AddSingleton<T1, T2>();
        }

        void IDependencyContainer.RegisterTransient<T1, T2>()
        {
            ServiceDescriptors.AddTransient<T1, T2>();
        }
    }
}
