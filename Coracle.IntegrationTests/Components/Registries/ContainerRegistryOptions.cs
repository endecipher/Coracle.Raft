using EventGuidance.Dependency;

namespace Coracle.IntegrationTests.Components.Registries
{
    public class ContainerRegistryOptions
    {
        public bool ConfigureHandler { get; set; }
        public Action<IDependencyContainer> HandlerAction { get; set; }
    }
}