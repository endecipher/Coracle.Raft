using ActivityLogger.Logging;
using Coracle.Raft.Engine.Discovery.Registrar;
using Coracle.Web.Discovery.Coracle.Logging;
using Coracle.Web.Discovery.Coracle.Registrar;

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


            services.Configure<DiscoveryLoggerOptions>(options =>
            {
                options.ConfigureHandler = false;
            });

            services.AddScoped<INodeRegistrar, NodeRegistrar>();
            services.AddSingleton<INodeRegistry, NodeRegistry>();
            services.AddSingleton<IActivityLogger, DiscoveryLoggerImpl>();
            services.AddSignalR();

            //services.AddDefaultCorrelationId();

            //services.AddMvc();//.AddRazorRuntimeCompilation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

            //app.UseCorrelationId();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapDefaultControllerRoute();
                //endpoints.MapControllers();

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Discovery}/{action=Get}/"
                );
            });
        }
    }
}
