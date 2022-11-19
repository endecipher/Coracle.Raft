using EventGuidance.Dependency;

namespace Coracle.Web.Registries
{
    public class ContainerRegistryOptions
    {
        public bool ConfigureHandler { get; set; }
        public Action<IDependencyContainer> HandlerAction { get; set; }
    }
}