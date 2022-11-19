namespace Coracle.Web.Coracle.Remoting
{
    public interface IAppInfo
    {
        Uri GetCurrentAppUri(); 
    }

    public class AppInfo : IAppInfo
    {
        public AppInfo(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }
        public IHttpContextAccessor HttpContextAccessor { get; }

        Uri IAppInfo.GetCurrentAppUri()
        {
            var request = HttpContextAccessor.HttpContext.Request;

            var host = request.Host.ToUriComponent();

            var pathBase = request.PathBase.ToUriComponent();

            return new Uri($"{request.Scheme}://{host}{pathBase}");
        }
    }
}
