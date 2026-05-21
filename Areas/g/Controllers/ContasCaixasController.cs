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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Default")]
    public partial class ContasCaixasController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_ContasCaixas";

        public ContasCaixasController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Contas Caixas";
            var model = new CstContasCaixasIndex
            {
                ContasCaixasIndex_id = String.Empty,
                ContasCaixasIndex_nome = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, nomeRestore;
            if (TryParseFiltroContasCaixasSemicolon(filtroPersistido.sql_filtro, out idRestore, out nomeRestore))
            {
                model.ContasCaixasIndex_id = idRestore;
                model.ContasCaixasIndex_nome = nomeRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(nomeRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionread")]
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

                var baseQuery = db.g_contas_caixas.AsNoTracking().Where(c => c.id_conta_caixa > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string nomeStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroContasCaixasSemicolon(recordFiltro.sql_filtro, out idStr, out nomeStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);
                }

                if (!listarTodosExplicito && !hasInline)
                {
                    return Json(new
                    {
                        errorMessage = "",
                        stackTrace = "",
                        yesFilterOnOff = "0",
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = 0,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                IQueryable<Db.g_contas_caixas> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroContasCaixasNaQuery(query, idStr, nomeStr);
                    LibDB.setFilterByUser(MontarFiltroContasCaixasPersistido(idStr, nomeStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoContasCaixasNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(c => new { c.id_conta_caixa, c.nome, c.banco, c.agencia, c.dv_agencia, c.conta, c.dv_conta, c.boleto_emissao })
                    .ToList();

                var list = page.Select(c =>
                {
                    string _boleto = c.boleto_emissao == true
                        ? LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg")
                        : LibIcons.getIcon("fa-regular fa-thumbs-down", "", "cc0000", "");
                    return new[]
                    {
                        "",
                        c.id_conta_caixa.ToString(),
                        c.nome ?? "",
                        c.banco ?? "",
                        (c.agencia ?? "") + "-" + (c.dv_agencia ?? ""),
                        (c.conta ?? "") + "-" + (c.dv_conta ?? ""),
                        _boleto
                    };
                }).ToList();

                filterOnOff = filterApplied ? "1" : "0";

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
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

        private static bool TryParseFiltroContasCaixasSemicolon(string raw, out string id, out string nome)
        {
            id = nome = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            nome = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(nome);
        }

        private static string MontarFiltroContasCaixasPersistido(string id, string nome)
        {
            return (id ?? String.Empty) + ";" + (nome ?? String.Empty);
        }

        private static IQueryable<Db.g_contas_caixas> AplicarFiltroContasCaixasNaQuery(IQueryable<Db.g_contas_caixas> query, string idStr, string nomeStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idConta))
            {
                query = query.Where(c => c.id_conta_caixa == idConta);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(nomeStr, out string padraoNome))
            {
                query = query.Where(c => c.nome != null && DbFunctions.Like(c.nome, padraoNome));
            }
            return query;
        }

        private static IQueryable<Db.g_contas_caixas> AplicarOrdenacaoContasCaixasNaQuery(IQueryable<Db.g_contas_caixas> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 2)
                {
                    return asc ? query.OrderBy(c => c.nome) : query.OrderByDescending(c => c.nome);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(c => c.id_conta_caixa) : query.OrderByDescending(c => c.id_conta_caixa);
                }
            }
            return query.OrderBy(c => c.nome);
        }
        #endregion

        #region Create
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b";
            PreencherLookupsCreateEdit();
            g_contas_caixas newRecord = new g_contas_caixas();
            newRecord.is_gerencial = false;
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actioncreate")]
        public ActionResult Create(g_contas_caixas record_g_contas_caixas)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b";
            record_g_contas_caixas.id_coligada = 1;
            record_g_contas_caixas.id_filial = 1;
            if (record_g_contas_caixas.nome.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }
            if (record_g_contas_caixas.nome_fantasia.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome_fantasia = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }

            // Validações Customizadas
            if (record_g_contas_caixas.cnpj != null)
            {
                if (!(LibStringValidate.ValidarCNPJ(record_g_contas_caixas.cnpj)))
                { ModelState.AddModelError("Model", "Campo [CNPJ] contém um CNPJ inválido"); }
            }

            if (ModelState.IsValid)
            {
                IQueryable<g_contas_caixas> listaContasCaixas = db.g_contas_caixas.Where(p => p.nome == record_g_contas_caixas.nome);
                foreach (g_contas_caixas validacao in listaContasCaixas)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_contas_caixas.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_contas_caixas.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_contas_caixas.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_contas_caixas.Add(record_g_contas_caixas);
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
            return View("CreateEdit", record_g_contas_caixas);
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_contas_caixas record_g_contas_caixas = db.g_contas_caixas.Find(id);
            if (record_g_contas_caixas == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b>" + LibStringFormat.GetTabHtml(1) + record_g_contas_caixas.id_conta_caixa.EmptyIfNull().ToString() + " - " + record_g_contas_caixas.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_contas_caixas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionupdate")]
        public ActionResult Edit(g_contas_caixas record_g_contas_caixas)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            if (ModelState.IsValid)
            {
                if (record_g_contas_caixas.nome.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }
                if (record_g_contas_caixas.nome_fantasia.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome_fantasia = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }

                IQueryable<g_contas_caixas> listaCidades = db.g_contas_caixas.Where(p => (p.nome == record_g_contas_caixas.nome) && (p.id_conta_caixa != record_g_contas_caixas.id_conta_caixa));
                foreach (g_contas_caixas validacao in listaCidades)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_contas_caixas.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_contas_caixas.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_contas_caixas.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_contas_caixas).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b>" + LibStringFormat.GetTabHtml(1) + record_g_contas_caixas.id_conta_caixa.EmptyIfNull().ToString() + " - " + record_g_contas_caixas.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_contas_caixas);
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