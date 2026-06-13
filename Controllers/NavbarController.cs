using System;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Domain;
using GdiPlataform.Security;

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
                    ContextoModel contextoModel = CachePersister.contextoModel;

                    // Recuperação do menu: o cache contextoModel_{TokenId} pode expirar (sliding 15 min)
                    // enquanto a sessão segue viva via Ajax/lookups, sendo recriado VAZIO por
                    // LookupQueryServiceCache.EnsureContextoModel(). Aqui remontamos o menu pelo método
                    // oficial do login (getNavbarItemsMenu) e re-persistimos, evitando a sidebar sem itens.
                    if ((contextoModel.allNavbarItemMenu == null || !contextoModel.allNavbarItemMenu.Any())
                        && CachePersister.userIdentity != null)
                    {
                        contextoModel.allNavbarItemMenu = new Contexto().getNavbarItemsMenu().ToList();
                        CachePersister.contextoModel = contextoModel;
                    }

                    NavbarFragmentCache.ApplyToContextoModel(contextoModel);
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
                ContextoModel contextoModel = CachePersister.contextoModel;
                // PERF-003: Index já preencheu o mesmo contextoModel nesta request — evita 2.ª passagem cache/clone.
                if (!NavbarFragmentCache.IsLoadedThisRequest())
                    NavbarFragmentCache.ApplyToContextoModel(contextoModel);
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