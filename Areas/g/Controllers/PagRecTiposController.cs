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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos_*,g_PagRecTipos_Default")]
    public class PagRecTiposController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_PagRecTipos";

        public PagRecTiposController()
        {
            String Inicio = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos,g_PagRecTipos_*,g_PagRecTipos_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Pag/Rec Tipos";
            var model = new CstPagRecTiposIndex
            {
                PagRecTiposIndex_id = String.Empty,
                PagRecTiposIndex_descricao = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, descRestore;
            if (TryParseFiltroPagRecTiposSemicolon(filtroPersistido.sql_filtro, out idRestore, out descRestore))
            {
                model.PagRecTiposIndex_id = idRestore;
                model.PagRecTiposIndex_descricao = descRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(descRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos,g_PagRecTipos_*,g_PagRecTipos_Actionread")]
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

                var baseQuery = db.g_pagrec_tipos.AsNoTracking().Where(p => p.id_pagrec_tipo > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string descStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroPagRecTiposSemicolon(recordFiltro.sql_filtro, out idStr, out descStr);
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

                IQueryable<Db.g_pagrec_tipos> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroPagRecTiposNaQuery(query, idStr, descStr);
                    LibDB.setFilterByUser(MontarFiltroPagRecTiposPersistido(idStr, descStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoPagRecTiposNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(p => new { p.id_pagrec_tipo, p.ativo, p.descricao, p.pagamento, p.recebimento, p.baixa_automatica })
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
                    string _baixaAutomatica = p.baixa_automatica == true
                        ? LibIcons.getIcon("fa-regular fa-thumbs-up", "Baixa Automática Ativada", "#008000", "fa-lg")
                        : LibIcons.getIcon("fa-regular fa-thumbs-down", "Baixa Automática Desativada", "cc0000", "");
                    return new[]
                    {
                        "",
                        p.id_pagrec_tipo.ToString(),
                        _ativo,
                        p.descricao ?? "",
                        _pagamento,
                        _recebimento,
                        _baixaAutomatica
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

        private static bool TryParseFiltroPagRecTiposSemicolon(string raw, out string id, out string descricao)
        {
            id = descricao = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            descricao = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(descricao);
        }

        private static string MontarFiltroPagRecTiposPersistido(string id, string descricao)
        {
            return (id ?? String.Empty) + ";" + (descricao ?? String.Empty);
        }

        private static IQueryable<Db.g_pagrec_tipos> AplicarFiltroPagRecTiposNaQuery(IQueryable<Db.g_pagrec_tipos> query, string idStr, string descStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idTipo))
            {
                query = query.Where(p => p.id_pagrec_tipo == idTipo);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(descStr, out string padraoDesc))
            {
                query = query.Where(p => p.descricao != null && DbFunctions.Like(p.descricao, padraoDesc));
            }
            return query;
        }

        private static IQueryable<Db.g_pagrec_tipos> AplicarOrdenacaoPagRecTiposNaQuery(IQueryable<Db.g_pagrec_tipos> query, jQueryDataTableParamModel param)
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
                    return asc ? query.OrderBy(p => p.id_pagrec_tipo) : query.OrderByDescending(p => p.id_pagrec_tipo);
                }
            }
            return query.OrderBy(p => p.id_pagrec_tipo);
        }
        #endregion

        #region ModalCreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos_*,g_PagRecTipos_Actioncreate,g_PagRecTipos_Actionupdate")]
        public ActionResult ModalCreateEditPagRecTipo(int? IdPagRecTipo)
        {
            try
            {
                int id = IdPagRecTipo.GetValueOrDefault();
                g_pagrec_tipos record;
                if (id > 0)
                {
                    record = db.g_pagrec_tipos.Find(id);
                    if (record == null)
                    {
                        ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Pag/Rec Tipo", id);
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Tipo — (não localizado)</b>";
                        return View("ModalCreateEditPagRecTipo", new g_pagrec_tipos { id_pagrec_tipo = id });
                    }
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Tipo</b>" + LibStringFormat.GetTabHtml(1) + record.id_pagrec_tipo.EmptyIfNull().ToString() + " - " + record.descricao.EmptyIfNull().ToString();
                }
                else
                {
                    record = new g_pagrec_tipos { ativo = true };
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Tipo</b>";
                }
                return View("ModalCreateEditPagRecTipo", record);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "PagRecTiposController";
                msg += "<br/>" + "ModalCreateEditPagRecTipo";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos_*,g_PagRecTipos_Actioncreate,g_PagRecTipos_Actionupdate")]
        public ActionResult AjaxCreateEditPagRecTipo(g_pagrec_tipos record_g_pagrec_tipos)
        {
            try
            {
                record_g_pagrec_tipos.descricao = (record_g_pagrec_tipos.descricao ?? "").Trim().ToUpper();

                if (ModelState.IsValid)
                {
                    IQueryable<g_pagrec_tipos> lista = record_g_pagrec_tipos.id_pagrec_tipo > 0
                        ? db.g_pagrec_tipos.Where(p => p.descricao == record_g_pagrec_tipos.descricao && p.id_pagrec_tipo != record_g_pagrec_tipos.id_pagrec_tipo)
                        : db.g_pagrec_tipos.Where(p => p.descricao == record_g_pagrec_tipos.descricao);
                    foreach (g_pagrec_tipos validacao in lista)
                    {
                        if (validacao.descricao.ToString().ToUpper().Equals(record_g_pagrec_tipos.descricao.ToString().ToUpper()))
                        { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                    }
                }

                if (!ModelState.IsValid)
                {
                    string msgErro = String.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage));
                    return Json(GdiMvcJsonResults.AjaxFailure(msgErro), JsonRequestBehavior.AllowGet);
                }

                if (record_g_pagrec_tipos.id_pagrec_tipo == 0)
                {
                    record_g_pagrec_tipos.id_coligada = 1;
                    record_g_pagrec_tipos.id_filial = 1;
                    record_g_pagrec_tipos.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                    record_g_pagrec_tipos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.g_pagrec_tipos.Add(record_g_pagrec_tipos);
                    db.SaveChanges();
                    return Json(new { success = true, msg = "Tipo <b>" + record_g_pagrec_tipos.descricao + "</b> cadastrado com sucesso!" }, JsonRequestBehavior.AllowGet);
                }

                record_g_pagrec_tipos.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_pagrec_tipos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_pagrec_tipos).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, msg = "Tipo <b>" + record_g_pagrec_tipos.descricao + "</b> atualizado com sucesso!" }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
                return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(GdiMvcJsonResults.AjaxFailure(e), JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos_*,g_PagRecTipos_Actioncreate")]
        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos_*,g_PagRecTipos_Actioncreate")]
        public ActionResult Create(g_pagrec_tipos record_g_pagrec_tipos)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Tipo</b";
            record_g_pagrec_tipos.id_coligada = 1;
            record_g_pagrec_tipos.id_filial = 1;
            record_g_pagrec_tipos.descricao = record_g_pagrec_tipos.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_pagrec_tipos> listaPagRecTipo = db.g_pagrec_tipos.Where(p => p.descricao == record_g_pagrec_tipos.descricao);
                foreach (g_pagrec_tipos validacao in listaPagRecTipo)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_g_pagrec_tipos.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_pagrec_tipos.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_pagrec_tipos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_pagrec_tipos.Add(record_g_pagrec_tipos);
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

            return View("CreateEdit", record_g_pagrec_tipos);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos_*,g_PagRecTipos_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecTipos_*,g_PagRecTipos_Actionupdate")]
        public ActionResult Edit(g_pagrec_tipos record_g_pagrec_tipos)
        {
            record_g_pagrec_tipos.descricao = record_g_pagrec_tipos.descricao.Trim().ToUpper();
            if (ModelState.IsValid)
            {
                IQueryable<g_pagrec_tipos> listaPagRecTipo = db.g_pagrec_tipos.Where(p => (p.descricao == record_g_pagrec_tipos.descricao) && (p.id_pagrec_tipo != record_g_pagrec_tipos.id_pagrec_tipo));
                foreach (g_pagrec_tipos validacao in listaPagRecTipo)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_g_pagrec_tipos.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_pagrec_tipos.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_pagrec_tipos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_pagrec_tipos).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Tipo</b>" + LibStringFormat.GetTabHtml(1) + record_g_pagrec_tipos.id_pagrec_tipo.EmptyIfNull().ToString() + " - " + record_g_pagrec_tipos.descricao.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_pagrec_tipos);
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