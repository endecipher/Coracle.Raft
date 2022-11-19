using Core.Raft.Extensions.Canoe.Remoting;
using Raft.Web.Canoe.ClientHandling;
using Raft.Web.Canoe.ClientHandling.Notes;
using Raft.Web.Canoe.Configuration;
using Raft.Web.Canoe.Logging;
using Raft.Web.Canoe.Node;
using Raft.Web.Canoe.PersistentData;
using Raft.Web.Hubs;
using StructureMap;

namespace Raft.Web
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
            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();

            services.AddSingleton<INode, CanoeNodeAccessor>();
            services.AddOptions<SettingOptions>();
            services.AddSignalR(options=>
            {
                
            });

            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-6.0&tabs=windows
            if (env.IsDevelopment())
            {
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapHub<LogHub>("/logHub");
            });
        }
    }
}
