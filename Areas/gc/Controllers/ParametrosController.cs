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
    public class ParametrosController : Controller
    {
        private GdiPlataformEntities db;
        public ParametrosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-sliders", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Parâmetros Comerciais";
            return View();
        }
    }
}