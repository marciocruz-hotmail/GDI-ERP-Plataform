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

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Default")]
    public class FiliaisController : Controller
    {
        private GdiPlataformEntities db;

        public FiliaisController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Filiais";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_filiais>();
            var allRecordsColigadas = db.g_coligadas.Select(g => new { g.id_coligada, g.razao_social }).ToList();

            // Perfil Adm visualiza todos os registros independente de Coligada e Filial
            if (CachePersister.userIdentity.IdPerfil == 1)
            { allRecords = db.g_filiais.Where(p => p.id_filial > 0).OrderBy(p => p.nome).ToList(); }
            else
            { allRecords = db.g_filiais.Where(f => f.id_filial > 0).OrderBy(p => p.nome).ToList(); }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_filiais, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_filial) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.nome :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_filial); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.nome); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_filial); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.nome); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                var recordGColigadas = allRecordsColigadas.Find(f => f.id_coligada == c.id_coligada);

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_filial.ToString(),
                                    c.nome.EmptyIfNull().ToString(),
                                    (recordGColigadas != null ? recordGColigadas.razao_social.EmptyIfNull().ToString() : String.Empty)
                                });
            }

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        #region PreencherLookupsCreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actioncreate,g_Filiais_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            var comboColigadas = new List<SelectListItem>();
            try
            {
                IQueryable<g_coligadas> listaDbColigadas = db.g_coligadas.Select(p => p).OrderBy(p => p.razao_social);
                foreach (g_coligadas item1 in listaDbColigadas)
                {
                    comboColigadas.Add(new SelectListItem { Value = item1.id_coligada.ToString(), Text = item1.razao_social.ToString() });
                }
            }
            finally { }
            ViewBag.comboColigadas = comboColigadas;
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Filial</b>";
            g_filiais newRecord = new g_filiais();
            PreencherLookupsCreateEdit();
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actioncreate")]
        public ActionResult Create(g_filiais record_g_filiais)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Filial</b>";
            record_g_filiais.nome = LibStringFormat.RemoverAcentos(record_g_filiais.nome);
            record_g_filiais.nome = record_g_filiais.nome.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_filiais> listaGFiliais = db.g_filiais.Where(p => p.nome == record_g_filiais.nome);
                foreach (g_filiais validacao in listaGFiliais)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_filiais.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_filiais.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_filiais.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_filiais.Add(record_g_filiais);
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
            PreencherLookupsCreateEdit();
            return View("CreateEdit", record_g_filiais);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_filiais record_g_filiais = db.g_filiais.Find(id);
            if (record_g_filiais == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = MontarTituloCreateEditFilial(record_g_filiais);
            return View("CreateEdit", record_g_filiais);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionupdate")]
        public ActionResult Edit(g_filiais record_g_filiais)
        {
            g_filiais existente = db.g_filiais.Find(record_g_filiais.id_filial);
            if (existente == null)
            {
                return RedirectToAction("Index");
            }

            string nomeNormalizado = LibStringFormat.RemoverAcentos(record_g_filiais.nome.EmptyIfNull().ToString()).Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_filiais> listaGFiliais = db.g_filiais.Where(p => (p.nome == nomeNormalizado) && (p.id_filial != existente.id_filial));
                foreach (g_filiais validacao in listaGFiliais)
                {
                    if (validacao.nome.ToString().ToUpper().Equals(nomeNormalizado))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                existente.nome = nomeNormalizado;
                existente.id_coligada = record_g_filiais.id_coligada;
                existente.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                existente.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
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

            record_g_filiais.nome = nomeNormalizado;
            PreencherLookupsCreateEdit();
            ViewBag.Title = MontarTituloCreateEditFilial(record_g_filiais);
            return View("CreateEdit", record_g_filiais);
        }

        private static string MontarTituloCreateEditFilial(g_filiais record)
        {
            return LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp"
                + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Filial</b>"
                + LibStringFormat.GetTabHtml(1) + record.id_filial.ToString() + " - " + record.nome.EmptyIfNull().ToString();
        }
        #endregion

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = yesFilterOnOff ?? "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }

    }
}