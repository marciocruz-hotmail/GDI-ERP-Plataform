using System.Web.Mvc;
using GdiPlataform.Security;

namespace GdiPlataform.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,Home")]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}