using Coracle.Web.Impl.Node;
using Microsoft.AspNetCore.Mvc;

namespace Coracle.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public string Ready([FromServices] ICoracleNodeAccessor coracleAccessor)
        {
            coracleAccessor.Ready();

            return coracleAccessor.EngineConfig.NodeId;
        }
    }
}