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

        public IActionResult Ready([FromServices] ICoracleNodeAccessor coracleAccessor)
        {
            coracleAccessor.Ready();

            return StatusCode(200);
        }
    }
}