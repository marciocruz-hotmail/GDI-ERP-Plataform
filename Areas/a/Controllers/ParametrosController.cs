using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.a.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,a_Parametros")]
    public class ParametrosController : Controller
    {
        private GdiPlataform.Db.GdiPlataformEntities db;

        public ParametrosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Parâmetros</b>";
            return View();
        }

        public ActionResult GetDadosSistemas(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            var allRecords = (from _a in db.a_sistemas
                              orderby _a.id_sistema
                              select new { sistemas = _a }).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);

            List<string[]> list = new List<string[]>();
            foreach (var a in displayedRecords)
            {
                String _ativo = a.sistemas.ativo == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");

                list.Add(new[] {
                                    a.sistemas.id_sistema.ToString(),
                                    a.sistemas.nome.ToString(),
                                    a.sistemas.sigla.ToString(),
                                    _ativo,
                                    "",
                                    ""
                                });
            }

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = "0",
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param);
            }
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxAtivarSistema(a_sistemas record_a_sistemas)
        {
            bool ativado = false;
            string msgRetorno = String.Empty;
            try
            {
                a_sistemas sistemaAtualizar = db.a_sistemas.Find(record_a_sistemas.id_sistema);
                sistemaAtualizar.ativo = true;
                db.Entry(sistemaAtualizar).State = EntityState.Modified;
                db.SaveChanges();
                ativado = true;
                msgRetorno = "Sistema <b>Ativado</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
            }
            catch (DbEntityValidationException ex)
            {
                ativado = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                ativado = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = ativado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxDesativarSistema(a_sistemas record_a_sistemas)
        {
            bool ativado = false;
            String msgRetorno = String.Empty;
            try
            {
                a_sistemas sistemaAtualizar = db.a_sistemas.Find(record_a_sistemas.id_sistema);
                sistemaAtualizar.ativo = false;
                db.Entry(sistemaAtualizar).State = EntityState.Modified;
                db.SaveChanges();
                ativado = true;
                msgRetorno = "Sistema DESATIVADO com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
            }
            catch (DbEntityValidationException ex)
            {
                ativado = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                ativado = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = ativado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null) { db.Dispose(); };
            }
            base.Dispose(disposing);
        }
    }
}