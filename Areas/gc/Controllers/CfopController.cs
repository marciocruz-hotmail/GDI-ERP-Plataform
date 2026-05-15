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

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Default")]
    public class CfopController : Controller
    {
        private GdiPlataformEntities db;

        public CfopController()
        {
            String Inicio = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop,gc_Cfop_*,gc_Cfop_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de CFOPs";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop,gc_Cfop_*,gc_Cfop_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            var allRecords = new List<Db.gc_cfop>(); // Lista vazia - Inicialização

            // Perfil Adm visualiza todos os registros independente de Coligada e Filial
            allRecords = db.gc_cfop.Where(p => p.id_cfop > 0).OrderBy(p => p.descricao).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_cfop.ToString(),
                                    _ativo,
                                    c.numero.ToString(),
                                    c.descricao.ToString(),
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
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actioncreate")]
        public ActionResult Create()
        {
            gc_cfop newRecord = new gc_cfop();
            newRecord.ativo = true;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b";
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actioncreate")]
        public ActionResult Create(gc_cfop record_gc_cfop)
        {
            record_gc_cfop.id_coligada = 1;
            record_gc_cfop.id_filial = 1;
            record_gc_cfop.descricao = record_gc_cfop.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<gc_cfop> listaCfop = db.gc_cfop.Where(p => p.descricao == record_gc_cfop.descricao);
                foreach (gc_cfop validacao in listaCfop)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_gc_cfop.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_gc_cfop.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_gc_cfop.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.gc_cfop.Add(record_gc_cfop);
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    ModelState.AddModelError("Model", LibExceptions.getDbEntityValidationException(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", LibExceptions.getExceptionShortMessage(e));
                }
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b";
            return View("CreateEdit", record_gc_cfop);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            gc_cfop record_g_cfop = db.gc_cfop.Find(id);
            if (record_g_cfop == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b>" + LibStringFormat.GetTabHtml(1) + record_g_cfop.id_cfop.EmptyIfNull().ToString() + " - " + record_g_cfop.numero.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_cfop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actionupdate")]
        public ActionResult Edit(gc_cfop record_gc_cfop)
        {
            record_gc_cfop.descricao = record_gc_cfop.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<gc_cfop> listaCfop = db.gc_cfop.Where(p => (p.descricao == record_gc_cfop.descricao) && (p.id_cfop != record_gc_cfop.id_cfop));
                foreach (gc_cfop validacao in listaCfop)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_gc_cfop.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_gc_cfop.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_gc_cfop.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_gc_cfop).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    ModelState.AddModelError("Model", LibExceptions.getDbEntityValidationException(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", LibExceptions.getExceptionShortMessage(e));
                }
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b>" + LibStringFormat.GetTabHtml(1) + record_gc_cfop.id_cfop.EmptyIfNull().ToString() + " - " + record_gc_cfop.numero.EmptyIfNull().ToString();
            return View("CreateEdit", record_gc_cfop);
        }
        #endregion

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