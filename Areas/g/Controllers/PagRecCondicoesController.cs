// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Default")]
    public class PagRecCondicoesController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_PagRecCondicoes";

        public PagRecCondicoesController()
        {
            String Inicio = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Pag/Rec Condições";
            var model = new CstPagRecCondicoesIndex
            {
                PagRecCondicoesIndex_id = String.Empty,
                PagRecCondicoesIndex_descricao = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, descRestore;
            if (TryParseFiltroPagRecCondicoesSemicolon(filtroPersistido.sql_filtro, out idRestore, out descRestore))
            {
                model.PagRecCondicoesIndex_id = idRestore;
                model.PagRecCondicoesIndex_descricao = descRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(descRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
                bool filterApplied = false;
                string yesFilterField = param.yesFilterField.EmptyIfNull().ToString().Trim();
                bool listarTodosExplicito = yesFilterField == "*";

                g_filtros recordFiltro;
                if (listarTodosExplicito)
                {
                    recordFiltro = LibDB.getFilterByUser(param, controllerName, db);
                }
                else
                {
                    recordFiltro = ObterFiltroPersistidoUsuario();
                }

                var baseQuery = db.g_pagrec_condicoes.AsNoTracking().Where(p => p.id_pagrec_condicao > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string descStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroPagRecCondicoesSemicolon(recordFiltro.sql_filtro, out idStr, out descStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);
                }

                if (!listarTodosExplicito && !hasInline)
                {
                    return Json(new
                    {
                        errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                        stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                        yesFilterOnOff = "0",
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = 0,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                IQueryable<Db.g_pagrec_condicoes> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroPagRecCondicoesNaQuery(query, idStr, descStr);
                    LibDB.setFilterByUser(MontarFiltroPagRecCondicoesPersistido(idStr, descStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoPagRecCondicoesNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(p => new { p.id_pagrec_condicao, p.ativo, p.descricao, p.pagamento, p.recebimento, p.qtd_dias, p.qtd_parcelas })
                    .ToList();

                var list = page.Select(p =>
                {
                    string _ativo = p.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                    string _pagamento = p.pagamento == true
                        ? LibIcons.getIcon("fa-regular fa-thumbs-up", "Habilitado para Pagamentos", "#008000", "fa-lg")
                        : LibIcons.getIcon("fa-regular fa-thumbs-down", "Desabilitado para Pagamentos", "cc0000", "");
                    string _recebimento = p.recebimento == true
                        ? LibIcons.getIcon("fa-regular fa-thumbs-up", "Habilitado para Recebimentos", "#008000", "fa-lg")
                        : LibIcons.getIcon("fa-regular fa-thumbs-down", "Desabilitado para Recebimentos", "cc0000", "");
                    return new[]
                    {
                        "",
                        p.id_pagrec_condicao.ToString(),
                        _ativo,
                        p.descricao ?? "",
                        _pagamento,
                        _recebimento,
                        p.qtd_dias.ToString(),
                        p.qtd_parcelas.ToString()
                    };
                }).ToList();

                filterOnOff = filterApplied ? "1" : "0";

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        private g_filtros ObterFiltroPersistidoUsuario()
        {
            if (CachePersister.userIdentity.allFiltros == null)
            {
                return new g_filtros();
            }
            string token = CachePersister.userIdentity.TokenAcesso.EmptyIfNull().ToString().Trim();
            g_filtros filtro = CachePersister.userIdentity.allFiltros
                .Where(f => f.token == token && f.controller == controllerName)
                .FirstOrDefault();
            return filtro ?? new g_filtros();
        }

        private static bool TryParseFiltroPagRecCondicoesSemicolon(string raw, out string id, out string descricao)
        {
            id = descricao = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            descricao = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(descricao);
        }

        private static string MontarFiltroPagRecCondicoesPersistido(string id, string descricao)
        {
            return (id ?? String.Empty) + ";" + (descricao ?? String.Empty);
        }

        private static IQueryable<Db.g_pagrec_condicoes> AplicarFiltroPagRecCondicoesNaQuery(IQueryable<Db.g_pagrec_condicoes> query, string idStr, string descStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idCondicao))
            {
                query = query.Where(p => p.id_pagrec_condicao == idCondicao);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(descStr, out string padraoDesc))
            {
                query = query.Where(p => p.descricao != null && DbFunctions.Like(p.descricao, padraoDesc));
            }
            return query;
        }

        private static IQueryable<Db.g_pagrec_condicoes> AplicarOrdenacaoPagRecCondicoesNaQuery(IQueryable<Db.g_pagrec_condicoes> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(p => p.descricao) : query.OrderByDescending(p => p.descricao);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(p => p.id_pagrec_condicao) : query.OrderByDescending(p => p.id_pagrec_condicao);
                }
            }
            return query.OrderBy(p => p.descricao);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b";
            g_pagrec_condicoes newRecord = new g_pagrec_condicoes();
            newRecord.ativo = true;
            newRecord.qtd_dias = 0;
            newRecord.qtd_parcelas = 0;
            return View("CreateEdit", newRecord);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actioncreate")]
        public ActionResult Create(g_pagrec_condicoes record_g_pagrec_condicoes)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b";
            record_g_pagrec_condicoes.id_coligada = 1;
            record_g_pagrec_condicoes.id_filial = 1;
            record_g_pagrec_condicoes.descricao = record_g_pagrec_condicoes.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_pagrec_condicoes> listaPagRecCondicao = db.g_pagrec_condicoes.Where(p => p.descricao == record_g_pagrec_condicoes.descricao);
                foreach (g_pagrec_condicoes validacao in listaPagRecCondicao)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_g_pagrec_condicoes.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_pagrec_condicoes.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_pagrec_condicoes.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_pagrec_condicoes.Add(record_g_pagrec_condicoes);
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureValidationMessage(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureMessage(e));
                }
            }
            return View("CreateEdit", record_g_pagrec_condicoes);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_pagrec_condicoes record_g_pagrec_condicoes = db.g_pagrec_condicoes.Find(id);
            if (record_g_pagrec_condicoes == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b>" + LibStringFormat.GetTabHtml(1) + record_g_pagrec_condicoes.id_pagrec_condicao.EmptyIfNull().ToString() + " - " + record_g_pagrec_condicoes.descricao.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_pagrec_condicoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionupdate")]
        public ActionResult Edit(g_pagrec_condicoes record_g_pagrec_condicoes)
        {
            record_g_pagrec_condicoes.descricao = record_g_pagrec_condicoes.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_pagrec_condicoes> listaPagRecCondicao = db.g_pagrec_condicoes.Where(p => (p.descricao == record_g_pagrec_condicoes.descricao) && (p.id_pagrec_condicao != record_g_pagrec_condicoes.id_pagrec_condicao));
                foreach (g_pagrec_condicoes validacao in listaPagRecCondicao)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_g_pagrec_condicoes.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_pagrec_condicoes.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_pagrec_condicoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_pagrec_condicoes).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureValidationMessage(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureMessage(e));
                }
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b>" + LibStringFormat.GetTabHtml(1) + record_g_pagrec_condicoes.id_pagrec_condicao.EmptyIfNull().ToString() + " - " + record_g_pagrec_condicoes.descricao.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_pagrec_condicoes);
        }
        #endregion

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
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