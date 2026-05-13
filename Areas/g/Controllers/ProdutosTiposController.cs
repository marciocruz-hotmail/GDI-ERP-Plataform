// Migrado em 2020_07_15

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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosTipos_*,g_ProdutosTipos_Default")]
    public class ProdutosTiposController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_ProdutosTipos";

        public ProdutosTiposController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosTipos_*,g_ProdutosTipos_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Tipos de Produtos";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosTipos_*,g_ProdutosTipos_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
            var allRecords = new List<Db.g_produtos_tipos>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                // Não há filtro
                allRecords = db.g_produtos_tipos.ToList();
            }
            if (filterDb)
            {
                SentencaSQL = string.Empty;
                if (record_g_filtro.advanced == true) { SentencaSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); }
                else { SentencaSQL = "select * from g_produtos_tipos where " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); };
                allRecords = db.g_produtos_tipos.SqlQuery(SentencaSQL).ToList();
            }
            else if (filterAdvanced)
            {
                // Filtro Avançado - Não implementado
                allRecords = db.g_produtos_tipos.ToList();
            }


            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_produtos_tipos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_produto_tipo) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.nome :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_produto_tipo); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.nome); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_produto_tipo); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.nome); }
                }
            }

            foreach (var c in displayedRecords)
            {
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_produto_tipo.ToString(),
                                    c.nome.ToString()
                                });
            }

            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

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

        #region Create Record
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosTipos_*,g_ProdutosTipos_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Tipo de Produto</b";
            g_produtos_tipos newRecord = new g_produtos_tipos();
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosTipos_*,g_ProdutosTipos_Actioncreate")]
        public ActionResult Create(g_produtos_tipos record_g_produtos_tipos)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Tipo de Produto</b";
            record_g_produtos_tipos.id_coligada = 1;
            record_g_produtos_tipos.id_filial = 1;

            if (ModelState.IsValid)
            {
                IQueryable<g_produtos_tipos> listaTiposProdutos = db.g_produtos_tipos.Where(p => p.nome == record_g_produtos_tipos.nome);
                foreach (g_produtos_tipos validacao in listaTiposProdutos)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_produtos_tipos.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_produtos_tipos.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_produtos_tipos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                record_g_produtos_tipos.nome = LibStringFormat.FormatarTextoSimples(record_g_produtos_tipos.nome);
                db.g_produtos_tipos.Add(record_g_produtos_tipos);
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

            return View("CreateEdit", record_g_produtos_tipos);
        }
        #endregion

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosTipos_*,g_ProdutosTipos_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_produtos_tipos record_g_produtos_tipos = db.g_produtos_tipos.Find(id);
            if (record_g_produtos_tipos == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Tipo de Produto</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos_tipos.id_produto_tipo.EmptyIfNull().ToString() + " - " + record_g_produtos_tipos.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_produtos_tipos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosTipos_*,g_ProdutosTipos_Actionupdate")]
        public ActionResult Edit(g_produtos_tipos record_g_produtos_tipos)
        {
            if (ModelState.IsValid)
            {
                record_g_produtos_tipos.nome = LibStringFormat.FormatarTextoSimples(record_g_produtos_tipos.nome);
                IQueryable <g_produtos_tipos> listaProdutos = db.g_produtos_tipos.Where(p => p.id_produto_tipo != record_g_produtos_tipos.id_produto_tipo);
                foreach (g_produtos_tipos validacao in listaProdutos)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_produtos_tipos.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_produtos_tipos.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_produtos_tipos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_produtos_tipos).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Tipo de Produto</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos_tipos.id_produto_tipo.EmptyIfNull().ToString() + " - " + record_g_produtos_tipos.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_produtos_tipos);
        }

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