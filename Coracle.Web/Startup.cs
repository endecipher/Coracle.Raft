using Coracle.Web.Configuration;
using Coracle.Web.Node;
using Coracle.Web.Hubs;
using CorrelationId.DependencyInjection;
using CorrelationId;
using Coracle.Web.Coracle.Remoting;
using StructureMap;
using EventGuidance.Dependency;
using Coracle.Web.Coracle.Registries;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Microsoft.Extensions.Options;
using Coracle.Web.Logging;
using Core.Raft.Canoe.Engine.Remoting;

namespace Coracle.Web
{
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

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();

            IConfigurationSection config = Configuration.GetSection("Coracle:EngineConfiguration");
            services.Configure<EngineConfigurationSettings>(config);


            services.Configure<ActivityLoggerOptions>(options =>
            {
                options.ConfigureHandler = false;
            });

            //services.AddSingleton<IDependencyContainer, StructureMapContainer>();
            services.AddSingleton<INode, CanoeNodeAccessor>();
            services.AddSingleton<IRemoteManager, HttpRemoteManager>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddOptions<SettingOptions>();

            services.AddSignalR();
            
            services.AddDefaultCorrelationId();

            services.AddMvc().AddRazorRuntimeCompilation();

            var container = new StructureMapContainer();
            container.Initialize();
            container.Populate(services);
            container.Scan();
            ComponentContainer.Instance = container;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-6.0&tabs=windows
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            
            app.UseStaticFiles();

            app.UseCorrelationId();

            app.UseRouting();

            app.UseAuthorization();

            //app.Use(async (context, next) =>
            //{
            //    await next.Invoke(context);
            //});

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LogHub>("/logHub");

                //endpoints.MapControllers();

                endpoints.MapDefaultControllerRoute();
                //endpoints.MapControllers();

                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=Home}/{action=Index}/{id?}"
                //);
            });
        }
    }
}
