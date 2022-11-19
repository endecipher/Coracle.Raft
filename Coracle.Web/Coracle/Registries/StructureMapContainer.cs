using EventGuidance.Dependency;
using StructureMap;

namespace Coracle.Web.Coracle.Registries
{
    public class StructureMapContainer : IDependencyContainer
    {
        bool IsInitialized { get; set; } = false;
        bool IsScanned { get; set; } = false;

        Container _container { get; set; }

        public void BuildUp(object target)
        {
            _container.BuildUp(target);
        }

        public void EjectAllInstancesOf<T>()
        {
            _container.EjectAllInstancesOf<T>();
        }

        public T GetInstance<T>()
        {
            return _container.GetInstance<T>();
        }

        public void Initialize()
        {
            if (!IsInitialized)
            {
                _container = new Container();
                IsInitialized = true;
            }
        }

        public void Scan()
        {
            System.Action<ConfigurationExpression> scanningAction = _ =>
            {
                _.Scan(assemblyScanner =>
                {
                    assemblyScanner.TheCallingAssembly();
                    assemblyScanner.LookForRegistries();
                    //assemblyScanner.WithDefaultConventions();
                    assemblyScanner.AssembliesAndExecutablesFromApplicationBaseDirectory();
                });
            };

            bool DependencyContainerNotInitialized = !IsInitialized;

            if (DependencyContainerNotInitialized)
                throw new InvalidOperationException(nameof(DependencyContainerNotInitialized));

            if (IsInitialized && !IsScanned)
            {
                _container.Configure(scanningAction);
                IsScanned = true;
            }
        }

        public void Populate(IServiceCollection services)
        {
            if (IsInitialized)
            {
                _container.Populate(services);
            }
        }

        public void InjectInstanceFor<T>(T instance)
        {
            _container.Configure((expr) => expr.For<T>().Singleton().Use(nameof(T), () => instance));
        }

        public void RegisterTransient<T1, T2>() where T2 : class, T1
        {
            _container.Configure(c => c.For<T1>().Use<T2>().Transient());
        }

        public void RegisterSingleton<T1, T2>() where T2 : class, T1
        {
            _container.Configure(c => c.For<T1>().Use<T2>().Singleton());
        }
    }
}
