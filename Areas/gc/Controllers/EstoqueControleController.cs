using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.GDI;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Default")]
    public partial class EstoqueControleController : Controller
    {
        private GdiPlataformEntities db;
        private readonly string controllerName = "gc_EstoqueControle";

        public EstoqueControleController()
        {
            String Inicio = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Produtos - Controles e Aferições";
            var model = new CstEstoqueControleIndex
            {
                EstoqueControleIndex_id = String.Empty,
                EstoqueControleIndex_serial = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, serialRestore;
            if (TryParseFiltroEstoqueControleSemicolon(filtroPersistido.sql_filtro, out idRestore, out serialRestore))
            {
                model.EstoqueControleIndex_id = idRestore;
                model.EstoqueControleIndex_serial = serialRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(serialRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle,gc_EstoqueControle_*,gc_EstoqueControle_Actionread")]
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

                var baseQuery = db.g_produtos_controle.AsNoTracking()
                    .Where(p => p.id_produto > 0 && p.ativo == true);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string serialStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(serialStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroEstoqueControleSemicolon(recordFiltro.sql_filtro, out idStr, out serialStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(serialStr);
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

                IQueryable<Db.g_produtos_controle> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroEstoqueControleNaQuery(query, idStr, serialStr);
                    LibDB.setFilterByUser(MontarFiltroEstoqueControlePersistido(idStr, serialStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoEstoqueControleNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(c => new
                    {
                        c.id_produto_controle,
                        c.serial,
                        c.id_produto,
                        c.id_produto_status,
                        c.lote,
                        c.data_validade
                    })
                    .ToList();

                var produtoIds = page.Where(c => c.id_produto > 0).Select(c => c.id_produto).Distinct().ToList();
                var statusIds = page.Where(c => c.id_produto_status > 0).Select(c => c.id_produto_status).Distinct().ToList();
                var produtosPorId = produtoIds.Count == 0
                    ? new Dictionary<int, string>()
                    : db.g_produtos.AsNoTracking()
                        .Where(p => produtoIds.Contains(p.id_produto))
                        .Select(p => new { p.id_produto, p.descricao })
                        .ToList()
                        .ToDictionary(p => p.id_produto, p => p.descricao ?? String.Empty);
                var statusPorId = statusIds.Count == 0
                    ? new Dictionary<int, string>()
                    : db.g_produtos_status.AsNoTracking()
                        .Where(s => statusIds.Contains(s.id_produto_status))
                        .Select(s => new { s.id_produto_status, s.descricao })
                        .ToList()
                        .ToDictionary(s => s.id_produto_status, s => s.descricao ?? String.Empty);

                var list = page.Select(c => new[]
                {
                    "",
                    c.id_produto_controle.ToString(),
                    c.serial.EmptyIfNull().ToString(),
                    produtosPorId.TryGetValue(c.id_produto, out string nomeProduto) ? nomeProduto : String.Empty,
                    statusPorId.TryGetValue(c.id_produto_status, out string nomeStatus) ? nomeStatus : String.Empty,
                    c.lote.EmptyIfNull().ToString(),
                    c.data_validade.HasValue ? c.data_validade.Value.ToString("dd/MM/yyyy") : ""
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

        private static bool TryParseFiltroEstoqueControleSemicolon(string raw, out string id, out string serial)
        {
            id = serial = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            serial = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(serial);
        }

        private static string MontarFiltroEstoqueControlePersistido(string id, string serial)
        {
            return (id ?? String.Empty) + ";" + (serial ?? String.Empty);
        }

        private static IQueryable<Db.g_produtos_controle> AplicarFiltroEstoqueControleNaQuery(
            IQueryable<Db.g_produtos_controle> query, string idStr, string serialStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idControle))
            {
                query = query.Where(c => c.id_produto_controle == idControle);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemCodigo(serialStr, out string padraoSerial))
            {
                query = query.Where(c => c.serial != null && DbFunctions.Like(c.serial, padraoSerial));
            }
            return query;
        }

        private static IQueryable<Db.g_produtos_controle> AplicarOrdenacaoEstoqueControleNaQuery(
            IQueryable<Db.g_produtos_controle> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 2)
                {
                    return asc ? query.OrderBy(c => c.serial) : query.OrderByDescending(c => c.serial);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(c => c.id_produto_controle) : query.OrderByDescending(c => c.id_produto_controle);
                }
            }
            return query.OrderBy(c => c.id_produto_controle);
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actioncreate,gc_EstoqueControle_Actionupdate")]
        public ActionResult ModalCreateEditEstoqueControle(int? IdProdutoControle)
        {
            try
            {
                int id = IdProdutoControle.GetValueOrDefault();
                if (id > 0)
                {
                    g_produtos_controle record = db.g_produtos_controle.Find(id);
                    if (record == null)
                    {
                        PreencherLookupsProdutosControleImportados();
                        ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Controle de Estoque", id);
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produtos - Controles e Aferições — (não localizado)</b>";
                        return View("ModalCreateEditEstoqueControle", new g_produtos_controle { id_produto_controle = id });
                    }
                    CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record);
                    PreencherLookupsProdutosControleImportados();
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produtos - Controles e Aferições</b>" + LibStringFormat.GetTabHtml(1) + record.id_produto_controle.EmptyIfNull().ToString() + " - " + record.serial.EmptyIfNull().ToString();
                    return View("ModalCreateEditEstoqueControle", record);
                }

                g_produtos_controle newRecord = new g_produtos_controle { ativo = true, id_coligada = 1, id_filial = 1 };
                PreencherLookupsProdutosControleCreate();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Produtos - Controles e Aferições (Novo)</b>";
                return View("ModalCreateEditEstoqueControle", newRecord);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "EstoqueControleController";
                msg += "<br/>" + "ModalCreateEditEstoqueControle";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actioncreate")]
        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actioncreate")]
        public ActionResult AjaxSaveRecord(g_produtos_controle view_g_produtos_controle)
        {
            int qtdInconsistencias = 0;
            String msgRetorno = "";
            bool sucesso = false;
            g_produtos_controle record_old_g_produtos_controle = new g_produtos_controle();

            try
            {
                if (view_g_produtos_controle.id_produto_controle > 0) { record_old_g_produtos_controle = JsonConvert.DeserializeObject<g_produtos_controle>(CachePersister.userIdentity.DataRowInUseSerialized); };

                if (ModelState.IsValid)
                {
                    if (view_g_produtos_controle.id_produto <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Produto]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                    if (view_g_produtos_controle.id_produto_familia <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Família]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                    if (view_g_produtos_controle.id_produto_status <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Status]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                    if (view_g_produtos_controle.serial.EmptyIfNull().ToString().Length <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Serial]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }


                if (qtdInconsistencias == 0)
                {
                    if (view_g_produtos_controle.id_produto_controle == 0)
                    {
                        view_g_produtos_controle.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                        view_g_produtos_controle.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                        db.g_produtos_controle.Add(view_g_produtos_controle);
                        db.SaveChanges();

                        String LogAlteracao = LibDB.CompareDataTable(record_old_g_produtos_controle, view_g_produtos_controle);
                        LogAlteracao = "Novo Registro | " + LogAlteracao;
                        LibAudit.SaveAudit(db, true,"g_produtos_controle", view_g_produtos_controle.id_produto_controle, LogAlteracao);
                        sucesso = true;
                    }
                    else
                    {
                        view_g_produtos_controle.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                        view_g_produtos_controle.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(view_g_produtos_controle).State = EntityState.Modified;
                        db.SaveChanges();

                        String LogAlteracao = LibDB.CompareDataTable(record_old_g_produtos_controle, view_g_produtos_controle);
                        LogAlteracao = "Atualização Dados | " + LogAlteracao;
                        LibAudit.SaveAudit(db, true,"g_produtos_controle", view_g_produtos_controle.id_produto_controle, LogAlteracao);
                        sucesso = true;
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            PreencherLookupsProdutosControleImportados();
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Produtos - Controles e Aferições";
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actionupdate")]
        public ActionResult Edit(g_produtos_controle record_g_produtos_controle)
        {
            record_g_produtos_controle.serial = record_g_produtos_controle.serial.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                record_g_produtos_controle.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_produtos_controle.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_produtos_controle).State = EntityState.Modified;
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
            PreencherLookupsProdutosControleImportados();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produtos - Controles e Aferições</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos_controle.id_produto_controle.EmptyIfNull().ToString() + " - " + record_g_produtos_controle.serial.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_produtos_controle);
        }
        #endregion

        #region Medicoes
        public ActionResult GetDadosMedicoes(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            string filterOnOff = "0";
            try
            {
                int IdProdutoControle = -1;
                int.TryParse(param.yesCustomIdPK, out IdProdutoControle);

                var allRecords = (from _m in db.g_produtos_medicoes
                                  join _u in db.g_usuarios on _m.id_usuario_cadastro equals _u.id_usuario into _U
                                  from _u in _U.DefaultIfEmpty()
                                  where (_m.id_produto_controle == IdProdutoControle && _m.ativo == true)
                                  orderby _m.datahora_cadastro
                                  select new { medicoes = _m, usuario = _u.nome.ToString() }).ToList();
                var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);

                List<string[]> list = new List<string[]>();
                foreach (var l in displayedRecords)
                {
                    list.Add(new[] {
                                        l.medicoes.id_produto_medicao.EmptyIfNull().ToString(),
                                        l.medicoes.data_medicao.ToString("dd/MM/yyyy"),
                                        l.medicoes.tensao.ToString(),
                                        "",
                                    });
                }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
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
                return Json(GdiMvcJsonResults.DataTableError(e, param, filterOnOff), JsonRequestBehavior.AllowGet);
            }
        }
        
        public ActionResult ModalCreateMedicao(int? id)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Registrar Medição/Aferição</b>";
            ViewBag.id = id;
            g_produtos_medicoes record_g_produtos_medicoes = new g_produtos_medicoes();
            record_g_produtos_medicoes.id_produto_controle = id.GetValueOrDefault();
            record_g_produtos_medicoes.data_medicao = LibDateTime.getDataHoraBrasilia();
            record_g_produtos_medicoes.ativo = true;
            record_g_produtos_medicoes.id_coligada = 1;
            record_g_produtos_medicoes.id_filial = 1;
            return View("ModalCreateMedicao", record_g_produtos_medicoes);
        }

        [HttpPost]
        public ActionResult AjaxCreateMedicao(g_produtos_medicoes view_g_produtos_medicoes)
        {
            bool cadastrado = false;
            int QtdErros = 0;
            String msgRetorno = String.Empty;
            try
            {
                if (view_g_produtos_medicoes.tensao.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    msgRetorno += "Campo <b>Tensão</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if (QtdErros == 0)
                {
                    view_g_produtos_medicoes.ativo = true;
                    view_g_produtos_medicoes.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                    view_g_produtos_medicoes.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                    view_g_produtos_medicoes.id_coligada = 1;
                    view_g_produtos_medicoes.id_filial = 1;
                    db.Entry(view_g_produtos_medicoes).State = EntityState.Added;
                    db.SaveChanges();
                    cadastrado = true;
                    msgRetorno = "Medição <b>Cadastrada</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                }
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = cadastrado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxCancelamentoMedicao(g_produtos_medicoes view_g_produtos_medicoes)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            try
            {
                g_produtos_medicoes record_g_produtos_medicoes = db.g_produtos_medicoes.Find(view_g_produtos_medicoes.id_produto_medicao);
                if (record_g_produtos_medicoes != null)
                {
                    record_g_produtos_medicoes.ativo = false;
                    record_g_produtos_medicoes.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    record_g_produtos_medicoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_g_produtos_medicoes).State = EntityState.Modified;
                    db.SaveChanges();
                    MsgRetorno = "Medição CANCELADA com sucesso!";
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
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

        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }

    }
}