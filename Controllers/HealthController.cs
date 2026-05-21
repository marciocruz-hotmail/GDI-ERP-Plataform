using System;
using System.Web.Mvc;
using GdiPlataform;

namespace GdiPlataform.Controllers
{
    /// <summary>
    /// Endpoint leve para IIS / load balancer / monitorização (sem sessão nem SQL).
    /// Rota curta: GET /health (ver RouteConfig).
    /// </summary>
    [AllowAnonymous]
    public class HealthController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            return Json(new
            {
                ok = true,
                app = "GDI-ERP-Plataform",
                version = ControlVersion.getShortVersion(),
                utc = DateTime.UtcNow.ToString("o")
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
