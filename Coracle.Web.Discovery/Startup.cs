using ActivityLogger.Logging;
using Coracle.Web.Discovery.Coracle.Logging;
using Coracle.Web.Discovery.Coracle.Registrar;
using Core.Raft.Canoe.Engine.Discovery.Registrar;
using CorrelationId.DependencyInjection;

namespace Coracle.Web.Discovery
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
            services.AddOptions();

            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();

            services.AddDefaultCorrelationId();

            services.Configure<DiscoveryLoggerOptions>(options =>
            {
                options.ConfigureHandler = false;
            });

            services.AddScoped<INodeRegistrar, NodeRegistrar>();
            services.AddSingleton<INodeRegistry, NodeRegistry>();
            services.AddSingleton<IActivityLogger, DiscoveryLoggerImpl>();
            services.AddSignalR();

            //services.AddDefaultCorrelationId();

            services.AddMvc();//.AddRazorRuntimeCompilation();

            //var container = new StructureMapContainer();
            //container.Initialize();
            //container.Populate(services);
            //container.Scan();
            //ComponentContainer.Instance = container;
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

            //app.UseCorrelationId();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapHub<LogHub>("/logHub");

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
