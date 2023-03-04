using ActivityLogger.Logging;
using Coracle.Samples.Registrar;
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
            services.AddEndpointsApiExplorer();
            services.AddScoped<INodeRegistrar, NodeRegistrar>();
            services.AddSingleton<INodeRegistry, NodeRegistry>();
            services.AddSingleton<IActivityLogger, DiscoveryActivityLogger>();
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Discovery}/{action=Get}/"
                );
            });
        }
    }
}
