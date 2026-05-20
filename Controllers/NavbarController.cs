using GdiPlataform.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Domain;
using GdiPlataform.Security;
using System.Runtime.Caching;
using GdiPlataform.Areas.gc.Models;

namespace GdiPlataform.Controllers
{
    public class NavbarController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                if (CachePersister.contextoModel != null)
                {
                    var contexto = new Contexto();
                    ContextoModel contextoModel = CachePersister.contextoModel;
                    contextoModel.allNavbarItemMessage = contexto.getNavbarItemsMessage().ToList();
                    contextoModel.allNavbarItemTask = contexto.getNavbarItemsTask().ToList();
                    contextoModel.allNavbarItemAlert = contexto.getNavbarItemsAlert().ToList();
                    contextoModel.allNavbarItemAtividade = contexto.getNavbarItemsAtividade().ToList();
                    contextoModel.userIdentity = CachePersister.userIdentity;
                    contextoModel.versaoPlataforma = ControlVersion.getVersion();
                    return PartialView("_Navbar", contextoModel);
                }
                else
                {
                    return PartialView("_Navbar");
                }
            }
            catch (Exception)
            {
                return PartialView("_Navbar");
            }
        }

        public ActionResult IndexFooter()
        {
            try
            {
                if (CachePersister.contextoModel == null)
                    return new EmptyResult();
                var contexto = new Contexto();
                ContextoModel contextoModel = CachePersister.contextoModel;
                contextoModel.allNavbarItemMessage = contexto.getNavbarItemsMessage().ToList();
                contextoModel.allNavbarItemTask = contexto.getNavbarItemsTask().ToList();
                contextoModel.allNavbarItemAlert = contexto.getNavbarItemsAlert().ToList();
                contextoModel.allNavbarItemAtividade = contexto.getNavbarItemsAtividade().ToList();
                contextoModel.userIdentity = CachePersister.userIdentity;
                contextoModel.versaoPlataforma = ControlVersion.getVersion();
                ViewBag.Version = ControlVersion.getShortVersion();
                return PartialView("_IndexFooter", contextoModel);
            }
            catch (Exception)
            {
                return new EmptyResult();
            }
        }

    }
}