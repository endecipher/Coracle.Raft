using Coracle.Web.Hubs;
using CorrelationId.DependencyInjection;
using CorrelationId;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using Coracle.Raft.Engine.Discovery;
using Coracle.Samples.ClientHandling;
using Coracle.Web.Impl.Logging;
using Coracle.Web.Impl.Discovery;
using Coracle.Web.Impl.Node;
using Coracle.Web.Impl.Remoting;
using TaskGuidance.BackgroundProcessing.Dependencies;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Command;
using Coracle.Samples.Data;
using Coracle.Web.Client;
using static Coracle.Web.Constants;

namespace Coracle.Web
{
    public sealed class EngineConfigurationOptions : EngineConfigurationSettings
    {
        public Uri DiscoveryServerUri { get; set; }
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddOptions();

            IConfigurationSection config = Configuration.GetSection(Strings.Configuration.EngineConfiguration);
            services.Configure<EngineConfigurationOptions>(config);

            new GuidanceDependencyRegistration().Register(new DotNetDependencyContainer(services));
            new Raft.Dependencies.Registration().Register(new DotNetDependencyContainer(services));

            services.AddSingleton<INoteStorage, NoteStorage>();
            services.AddSingleton<IOutboundRequestHandler, HttpOutboundRequestHandler>();
            services.AddSingleton<IStateMachineHandler, NoteKeeperStateMachineHandler>();
            services.AddSingleton<ISnapshotManager, SnapshotManager>();
            services.AddSingleton<IPersistentStateHandler, SampleVolatileStateHandler>();
            services.AddSingleton<ITaskProcessorConfiguration, TaskProcessorConfiguration>();
            services.AddSingleton<IActivityLogger, WebActivityLogger>();
            services.AddTransient<ICorrelationContextAccessor, CorrelationContextAccessor>();
            services.AddTransient<ICoracleClient, CoracleClient>();
            services.AddSingleton<IDiscoveryHandler, HttpDiscoveryHandler>();
            services.AddSingleton<ICoracleNodeAccessor, CoracleNodeAccessor>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddSingleton<IActivityLogger, WebActivityLogger>();

            services.AddSignalR();
            
            services.AddDefaultCorrelationId(options =>
            {
                options.IgnoreRequestHeader = true;
                options.EnforceHeader = false;
            });

            services.AddMvc().AddRazorRuntimeCompilation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();
            
            app.UseStaticFiles();

            app.UseCorrelationId();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LogHub>(Constants.Hubs.Log);
                endpoints.MapHub<RaftHub>(Constants.Hubs.Raft);

                endpoints.MapDefaultControllerRoute();
            });
        }


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
}
