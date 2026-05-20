using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Fretes_*,gc_Fretes_Default")]
    public partial class FretesController : Controller
    {
        private GdiPlataformEntities db;
        public FretesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        public ActionResult Index()
        {
            PreencherLookupsTransportadora();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-truck fa-lg", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>" + "Gestão de Fretes" + "</b>";
            return View();
        }
    }
}