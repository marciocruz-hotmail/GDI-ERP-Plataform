// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Default")]
    public partial class VendedoresController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Vendedores";

        public VendedoresController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Vendedores";
            var model = new CstVendedoresIndex
            {
                VendedoresIndex_id = String.Empty,
                VendedoresIndex_nome = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, nomeRestore;
            if (TryParseFiltroVendedoresSemicolon(filtroPersistido.sql_filtro, out idRestore, out nomeRestore))
            {
                model.VendedoresIndex_id = idRestore;
                model.VendedoresIndex_nome = nomeRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(nomeRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionread")]
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

                var baseQuery = db.g_vendedores.AsNoTracking().Where(v => v.id_vendedor > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string nomeStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroVendedoresSemicolon(recordFiltro.sql_filtro, out idStr, out nomeStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);
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

                IQueryable<Db.g_vendedores> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroVendedoresNaQuery(query, idStr, nomeStr);
                    LibDB.setFilterByUser(MontarFiltroVendedoresPersistido(idStr, nomeStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoVendedoresNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(v => new { v.id_vendedor, v.ativo, v.nome, v.email })
                    .ToList();

                var list = page.Select(v =>
                {
                    string _ativo = v.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                    return new[]
                    {
                        "",
                        v.id_vendedor.ToString(),
                        _ativo,
                        v.nome ?? "",
                        v.email ?? ""
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

        private static bool TryParseFiltroVendedoresSemicolon(string raw, out string id, out string nome)
        {
            id = nome = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            nome = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(nome);
        }

        private static string MontarFiltroVendedoresPersistido(string id, string nome)
        {
            return (id ?? String.Empty) + ";" + (nome ?? String.Empty);
        }

        private static IQueryable<Db.g_vendedores> AplicarFiltroVendedoresNaQuery(IQueryable<Db.g_vendedores> query, string idStr, string nomeStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idVendedor))
            {
                query = query.Where(v => v.id_vendedor == idVendedor);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(nomeStr, out string padraoNome))
            {
                query = query.Where(v => v.nome != null && DbFunctions.Like(v.nome, padraoNome));
            }
            return query;
        }

        private static IQueryable<Db.g_vendedores> AplicarOrdenacaoVendedoresNaQuery(IQueryable<Db.g_vendedores> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(v => v.nome) : query.OrderByDescending(v => v.nome);
                }
                if (param.iSortCol_0 == 4)
                {
                    return asc ? query.OrderBy(v => v.email) : query.OrderByDescending(v => v.email);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(v => v.id_vendedor) : query.OrderByDescending(v => v.id_vendedor);
                }
            }
            return query.OrderBy(v => v.id_vendedor);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actioncreate,g_Vendedores_Actionupdate")]
        public ActionResult ModalCreateEditVendedor(int? IdVendedor)
        {
            try
            {
                int idVendedor = IdVendedor.GetValueOrDefault();
                CstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel = new CstViewVendedoresTabelasModel();
                if (idVendedor > 0)
                {
                    g_vendedores record_g_vendedores = db.g_vendedores.Find(idVendedor);
                    if (record_g_vendedores == null)
                    {
                        return RedirectToAction("Index");
                    }
                    record_cstViewVendedoresTabelasDetalhesModel.g_vendedores = record_g_vendedores;
                    ViewBag.Title = MontarTituloCreateEditVendedor(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores);
                }
                else
                {
                    record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.ativo = true;
                    record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_revenda = 0;
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b>";
                }
                PreencherLookupsCreateEdit();
                return View("ModalCreateEditVendedor", record_cstViewVendedoresTabelasDetalhesModel);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "VendedoresController";
                msg += "<br/>" + "ModalCreateEditVendedor";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actioncreate,g_Vendedores_Actionupdate")]
        public ActionResult AjaxCreateEditVendedor(CstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel)
        {
            try
            {
                g_vendedores vendedor = record_cstViewVendedoresTabelasDetalhesModel.g_vendedores;

                if (vendedor.id_vendedor == 0)
                {
                    vendedor.id_coligada = 1;
                    vendedor.id_filial = 1;

                    if (ModelState.IsValid)
                    {
                        IQueryable<g_vendedores> listaVendedores = db.g_vendedores.Where(p => p.nome == vendedor.nome && p.id_coligada == vendedor.id_coligada && p.id_filial == vendedor.id_filial);
                        foreach (g_vendedores validacao in listaVendedores)
                        {
                            if (validacao.nome.ToString().ToUpper().Equals(vendedor.nome.ToString().ToUpper()))
                            { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        string msgErro = String.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage));
                        return Json(GdiMvcJsonResults.AjaxFailure(msgErro), JsonRequestBehavior.AllowGet);
                    }

                    vendedor.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                    vendedor.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.g_vendedores.Add(vendedor);
                    db.SaveChanges();
                    return Json(new { success = true, msg = "Vendedor <b>" + vendedor.nome.EmptyIfNull().ToString() + "</b> cadastrado com sucesso!" }, JsonRequestBehavior.AllowGet);
                }

                g_vendedores existente = db.g_vendedores.Find(vendedor.id_vendedor);
                if (existente == null)
                {
                    return Json(GdiMvcJsonResults.AjaxFailure(GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Vendedor", vendedor.id_vendedor)), JsonRequestBehavior.AllowGet);
                }

                if (ModelState.IsValid)
                {
                    IQueryable<g_vendedores> listaVendedores = db.g_vendedores.Where(p => (p.nome == vendedor.nome && p.id_coligada == vendedor.id_coligada && p.id_filial == vendedor.id_filial) && (p.id_vendedor != vendedor.id_vendedor));
                    foreach (g_vendedores validacao in listaVendedores)
                    {
                        if (validacao.nome.ToString().ToUpper().Equals(vendedor.nome.ToString().ToUpper()))
                        { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                    }
                }

                if (!ModelState.IsValid)
                {
                    string msgErro = String.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage));
                    return Json(GdiMvcJsonResults.AjaxFailure(msgErro), JsonRequestBehavior.AllowGet);
                }

                vendedor.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                vendedor.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                vendedor.nome = vendedor.nome.ToUpper();
                db.Entry(vendedor).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, msg = "Vendedor <b>" + vendedor.nome.EmptyIfNull().ToString() + "</b> atualizado com sucesso!" }, JsonRequestBehavior.AllowGet);
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

        private static string MontarTituloCreateEditVendedor(g_vendedores record)
        {
            return LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp"
                + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b>"
                + LibStringFormat.GetTabHtml(1) + record.id_vendedor.EmptyIfNull().ToString() + " - " + record.nome.EmptyIfNull().ToString();
        }

        #region Create
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actioncreate")]
        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actioncreate")]
        public ActionResult Create(CstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel)
        {
            PreencherLookupsCreateEdit();
            record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_coligada = 1;
            record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_filial = 1;
            //record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome = record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToUpper();
            if (ModelState.IsValid)
            {
                IQueryable<g_vendedores> listaVendedores = db.g_vendedores.Where(p => p.nome == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome && p.id_coligada == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_coligada && p.id_filial == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_filial);
                foreach (g_vendedores validacao in listaVendedores)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }
            if (ModelState.IsValid)
            {
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_vendedores.Add(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores);
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b";
            return View("CreateEdit", record_cstViewVendedoresTabelasDetalhesModel);
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionupdate")]
        public ActionResult Edit(CstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel)
        {
            PreencherLookupsCreateEdit();
            if (ModelState.IsValid)
            {
                IQueryable<g_vendedores> listaVendedores = db.g_vendedores.Where(p => (p.nome == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome && p.id_coligada == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_coligada && p.id_filial == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_filial) && (p.id_vendedor != record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_vendedor));
                foreach (g_vendedores validacao in listaVendedores)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }
            if (ModelState.IsValid)
            {

                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.datahora_alteracao = DataHoraAtual;
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome = record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToUpper();
                db.Entry(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b>" + LibStringFormat.GetTabHtml(1) + record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_vendedor.EmptyIfNull().ToString() + " - " + record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_cstViewVendedoresTabelasDetalhesModel);
        }
        #endregion
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