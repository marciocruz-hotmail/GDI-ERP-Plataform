using ClosedXML.Excel;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexProdutos_*,gc_ComexProdutos_Default,gc_ComexProdutos_ProdutosPre")]
    public partial class ComexProdutosController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "gc_ComexProdutos";
        public ComexProdutosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexProdutos_*,gc_ComexProdutos_Default,gc_ComexProdutos_ProdutosPre")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Produtos Comex";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexProdutos_*,gc_ComexProdutos_Default,gc_ComexProdutos_ProdutosPre")]
        public ActionResult ProdutosPre()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Produtos (Novos)";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexProdutos_*,gc_ComexProdutos_Default,gc_ComexProdutos_ProdutosPre")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            string errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage;
            string stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace;

            // Parâmetros/filtros
            bool filterDb = false;
            string filterOnOff = "0";

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                string sentencaSql = string.Empty;

                // Observação: LibDB.getFilterByUser parece depender do flag filterAdvanced;
                // aqui mantemos a chamada como no seu padrão.
                g_filtros recordFiltro = LibDB.getFilterByUser(param, controllerName, db);

                if (recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    filterDb = true;
                    filterOnOff = "1";
                }

                // ----------------------------------------
                // 1) Monta a "query base" (preferência LINQ)
                // ----------------------------------------
                IQueryable<Db.gc_comex_produtos> query = db.gc_comex_produtos.AsNoTracking();

                // Sempre ativo, como seu código original (exceto quando filtro advanced monta SQL já com ativo=1)
                query = query.Where(p => p.ativo == true);

                // ----------------------------------------
                // 2) Filtro persistido (filterDb)
                //    - você informou que o texto armazenado é o trecho pós-WHERE (ex.: "id_cliente = 157")
                //    - aqui mantemos como SQL, mas agora com paginação no SQL (não traz tudo)
                // ----------------------------------------
                if (filterDb)
                {
                    sentencaSql = recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim();

                    // Se NÃO for advanced, assume que é somente "depois do where"
                    if (recordFiltro.advanced != true)
                    {
                        sentencaSql = "SELECT * FROM gc_comex_produtos WHERE ativo = 1 AND " + sentencaSql;
                    }

                    // Total (COUNT) sem trazer tudo
                    string sqlCount = "SELECT COUNT(1) FROM (" + sentencaSql + ") T";
                    int total = db.Database.SqlQuery<int>(sqlCount).FirstOrDefault();

                    // Ordenação/paginação (DataTables)
                    bool asc = (param.sSortDir_0 ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                    int sortCol = param.iSortCol_0;

                    // No seu grid, você só ordena por id_produto quando sortCol == 1 (mesma regra)
                    // Obs: o campo usado no seu loop é c.id_produto (associação ao GDI) e também existe id_comex_produto.
                    // Aqui mantive id_produto, como seu código original.
                    string orderBy = " ORDER BY id_produto DESC";
                    if (param.iSortingCols > 0 && sortCol == 1)
                        orderBy = asc ? " ORDER BY id_produto ASC" : " ORDER BY id_produto DESC";

                    // Paginação SQL Server (OFFSET/FETCH)
                    string sqlPage =
                        "SELECT * FROM (" + sentencaSql + ") T " +
                        orderBy +
                        " OFFSET " + start + " ROWS FETCH NEXT " + length + " ROWS ONLY";

                    // Page
                    var page = db.gc_comex_produtos.SqlQuery(sqlPage).ToList();

                    // Lookup produtos GDI SOMENTE para IDs da página
                    var idsProduto = page.Where(x => x.id_produto > 0).Select(x => x.id_produto).Distinct().ToList();

                    var produtosMap = db.g_produtos.AsNoTracking()
                        .Where(p => p.ativo == true && idsProduto.Contains(p.id_produto))
                        .Select(p => new { p.id_produto, p.nome })
                        .ToList()
                        .ToDictionary(x => x.id_produto, x => x.nome);

                    // Monta aaData
                    var list = new List<string[]>(page.Count);
                    foreach (var c in page)
                    {
                        string ativoIcon = c.ativo == true
                            ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                            : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");

                        string descTraducao = c.description.EmptyIfNull().ToString();
                        if (c.traducao.EmptyIfNull().ToString().Length > 0)
                            descTraducao += "<br/>[" + c.traducao.EmptyIfNull().ToString() + "]";

                        string nomeProdutoGdi = "Novo Produto NÃO Associado!";
                        if (c.id_produto > 0 && produtosMap.TryGetValue(c.id_produto, out var nome))
                        {
                            nomeProdutoGdi = (nome ?? "");
                            if (nomeProdutoGdi.Length > 80) nomeProdutoGdi = nomeProdutoGdi.Substring(0, 80) + "...";
                        }

                        list.Add(new[]
                        {
                    "", // Seleção
                    c.id_comex_produto.ToString(),
                    ativoIcon,
                    c.pn.EmptyIfNull().ToString(),
                    descTraducao,
                    nomeProdutoGdi,
                    "" // Editar
                });
                    }

                    return Json(new
                    {
                        errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                        stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                        yesFilterOnOff = filterOnOff,
                        sEcho = param.sEcho,
                        iTotalRecords = total,
                        iTotalDisplayRecords = total,
                        aaData = list
                    }, JsonRequestBehavior.AllowGet);
                }

                // ----------------------------------------
                // 3) Totais + ordenação + paginação (LINQ) — listagem padrão
                // ----------------------------------------
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                bool ascLinq = (param.sSortDir_0 ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                int sortColLinq = param.iSortCol_0;

                IOrderedQueryable<Db.gc_comex_produtos> ordered = query.OrderByDescending(p => p.id_produto);

                if (param.iSortingCols > 0 && sortColLinq == 1)
                    ordered = ascLinq ? query.OrderBy(p => p.id_produto) : query.OrderByDescending(p => p.id_produto);

                var pageLinq = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(p => new
                    {
                        p.id_comex_produto,
                        p.ativo,
                        p.pn,
                        p.description,
                        p.traducao,
                        p.id_produto
                    })
                    .ToList();

                // Produtos GDI somente da página
                var idsProdutoPage = pageLinq.Where(x => x.id_produto > 0).Select(x => x.id_produto).Distinct().ToList();

                var produtosMapLinq = db.g_produtos.AsNoTracking()
                    .Where(p => p.ativo == true && idsProdutoPage.Contains(p.id_produto))
                    .Select(p => new { p.id_produto, p.nome })
                    .ToList()
                    .ToDictionary(x => x.id_produto, x => x.nome);

                var listOut = new List<string[]>(pageLinq.Count);

                foreach (var c in pageLinq)
                {
                    string ativoIcon = c.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");

                    string descTraducao = c.description.EmptyIfNull().ToString();
                    if (c.traducao.EmptyIfNull().ToString().Length > 0)
                        descTraducao += "<br/>[" + c.traducao.EmptyIfNull().ToString() + "]";

                    string nomeProdutoGdi = "Novo Produto NÃO Associado!";
                    if (c.id_produto > 0 && produtosMapLinq.TryGetValue(c.id_produto, out var nome))
                    {
                        nomeProdutoGdi = (nome ?? "");
                        if (nomeProdutoGdi.Length > 80) nomeProdutoGdi = nomeProdutoGdi.Substring(0, 80) + "...";
                    }

                    listOut.Add(new[]
                    {
                "", // Seleção
                c.id_comex_produto.ToString(),
                ativoIcon,
                c.pn.EmptyIfNull().ToString(),
                descTraducao,
                nomeProdutoGdi,
                "" // Editar
            });
                }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = listOut
                }, JsonRequestBehavior.AllowGet);
            }
                        catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }
        #endregion

        #region GetDadosProdutosPre
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexProdutos_*,gc_ComexProdutos_Default,gc_ComexProdutos_ProdutosPre")]
        public ActionResult GetDadosProdutosPre(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            string errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage;
            string stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace;

            string filterOnOff = "0";

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                g_filtros recordFiltro = LibDB.getFilterByUser(param, controllerName, db);

                // ----------------------------------------
                // 1) Query base (LINQ) - SEM SQL concatenado
                // ----------------------------------------
                var query = db.gc_comex_produtos
                    .AsNoTracking()
                    .Where(p => p.ativo == true)
                    .Where(p =>
                        (p.item_cadastro_novo == true && p.id_produto == 0) ||
                        (p.item_cadastro_atualizar == true && p.id_produto > 0)
                    );

                // Se a regra era "se IdVendedor > 0 filtra por usuario cadastro/alteracao"
                // (seu código original usa IdVendedor > 0 como gatilho, mas filtra por IdUsuario)
                if (CachePersister.userIdentity.IdVendedor > 0)
                {
                    int idUsuario = CachePersister.userIdentity.IdUsuario;
                    query = query.Where(p => p.id_usuario_cadastro == idUsuario || p.id_usuario_alteracao == idUsuario);
                }

                // Totais (DataTables)
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // ----------------------------------------
                // 2) Ordenação (DataTables) - precisa OrderBy ANTES do Skip (EF)
                // ----------------------------------------
                bool asc = (param.sSortDir_0 ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                int sortCol = param.iSortCol_0;

                // No seu código: só ordena por id_produto quando sortCol==1.
                // Caso contrário, defina um default estável (id_comex_produto desc)
                IOrderedQueryable<Db.gc_comex_produtos> ordered =
                    query.OrderByDescending(p => p.id_comex_produto);

                if (param.iSortingCols > 0 && sortCol == 1)
                    ordered = asc ? query.OrderBy(p => p.id_produto) : query.OrderByDescending(p => p.id_produto);

                // Página (pega só o necessário)
                var page = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(p => new
                    {
                        p.id_comex_produto,
                        p.ativo,
                        p.pn,
                        p.description,
                        p.traducao,
                        p.id_produto,
                        p.item_cadastro_novo,
                        p.item_cadastro_atualizar
                    })
                    .ToList();

                // ----------------------------------------
                // 3) Lookup Produtos GDI somente dos IDs da página
                // ----------------------------------------
                var idsProduto = page.Where(x => x.id_produto > 0).Select(x => x.id_produto).Distinct().ToList();

                var produtosMap = db.g_produtos
                    .AsNoTracking()
                    .Where(p => p.ativo == true && idsProduto.Contains(p.id_produto))
                    .Select(p => new { p.id_produto, p.nome })
                    .ToList()
                    .ToDictionary(x => x.id_produto, x => x.nome);

                // ----------------------------------------
                // 4) Monta aaData
                // ----------------------------------------
                var list = new List<string[]>(page.Count);

                foreach (var c in page)
                {
                    string ativoIcon = c.ativo
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");

                    string nomeProdutoGdi = "";
                    if (c.id_produto > 0 && produtosMap.TryGetValue(c.id_produto, out var nomeGdi))
                    {
                        nomeProdutoGdi = nomeGdi.EmptyIfNull().ToString();
                    }

                    // Nome do produto COMEX: tradução se existir, senão description
                    string nomeProdutoComex = "";
                    if (c.traducao.EmptyIfNull().ToString().Length > 0) nomeProdutoComex = c.traducao.EmptyIfNull().ToString();
                    else nomeProdutoComex = c.description.EmptyIfNull().ToString();

                    // Alertas (mesma lógica do seu código)
                    if (c.item_cadastro_novo || c.item_cadastro_atualizar) nomeProdutoComex += "<br/>";
                    if (c.item_cadastro_novo) nomeProdutoComex += "<font color=\"red\"><strong>Atenção: Validar/Cadastrar Novo Produto!</strong></font>";
                    if (c.item_cadastro_atualizar) nomeProdutoComex += "<font color=\"red\"><strong>Atenção: Validar/Atualizar Tradução!</strong></font>";

                    list.Add(new[]
                    {
                "", // Seleção
                c.id_comex_produto.ToString(),
                ativoIcon,
                c.pn.EmptyIfNull().ToString(),
                nomeProdutoGdi,
                nomeProdutoComex
            });
                }

                // Aqui o método não tem filtroDb/filterAdvanced de verdade.
                // Se quiser acender o botão quando houver algum critério (ex.: IdVendedor>0),
                // você pode fazer:
                // if (CachePersister.userIdentity.IdVendedor > 0) filterOnOff = "1";

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
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexProdutos_*,gc_ComexProdutos_Default,gc_ComexProdutos_ProdutosPre")]
        public ActionResult ModalCreateEdit(int? id)
        {
            try
            {
                int idProdutoComex = id.GetValueOrDefault();
                if (idProdutoComex <= 0)
                {
                    ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Produto COMEX", null);
                    PreencherLookupsProdutosServicosTodos();
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produto Comex — (não localizado)</b>";
                    return View("ModalCreateEdit", new gc_comex_produtos());
                }
                gc_comex_produtos record_gc_comex_produtos = db.gc_comex_produtos.Find(idProdutoComex);
                if (record_gc_comex_produtos == null)
                {
                    ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Produto COMEX", idProdutoComex);
                    PreencherLookupsProdutosServicosTodos();
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produto Comex — (não localizado)</b>";
                    return View("ModalCreateEdit", new gc_comex_produtos { id_comex_produto = idProdutoComex });
                }
                if (record_gc_comex_produtos.id_produto == 0)
                {
                    g_produtos record_g_produtos = db.g_produtos.Where(p => p.ativo == true && p.codigo == record_gc_comex_produtos.pn).FirstOrDefault();
                    if (record_g_produtos == null) { record_g_produtos = db.g_produtos.SqlQuery("select * from g_produtos where codigo like '" + record_gc_comex_produtos.pn.EmptyIfNull().ToString().Replace("0","_").Replace("O", "_").Replace("o", "_") + "'").FirstOrDefault(); };
                    if (record_g_produtos != null)
                    {
                        record_gc_comex_produtos.id_produto = record_g_produtos.id_produto;
                    }
                }
                CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record_gc_comex_produtos);
                PreencherLookupsProdutosServicosTodos();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produto Comex</b>" + LibStringFormat.GetTabHtml(1) + record_gc_comex_produtos.id_produto.EmptyIfNull().ToString() + " - " + record_gc_comex_produtos.description.EmptyIfNull().ToString();
                return View("ModalCreateEdit", record_gc_comex_produtos);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "ComexProdutosController";
                msg += "<br/>" + "ModalCreateEdit";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        public ActionResult AjaxCreateEdit(gc_comex_produtos view_record_gc_comex_produtos)
        {
            bool sucesso = false;
            String msgRetorno = "";
            gc_comex_produtos record_old_gc_comex_produtos = JsonConvert.DeserializeObject<gc_comex_produtos>(CachePersister.userIdentity.DataRowInUseSerialized);
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                if (ModelState.IsValid)
                {
                    if (view_record_gc_comex_produtos.pn.EmptyIfNull().ToString().Trim().Length == 0) { ModelState.AddModelError("Model", "Campo [PN] é de preenchimento obrigatório"); };
                    if (view_record_gc_comex_produtos.description.EmptyIfNull().ToString().Trim().Length == 0) { ModelState.AddModelError("Model", "Campo [Descrição] é de preenchimento obrigatório"); };
                    if (view_record_gc_comex_produtos.traducao.EmptyIfNull().ToString().Trim().Length == 0) { ModelState.AddModelError("Model", "Campo [Tradução] é de preenchimento obrigatório"); };
                    if (view_record_gc_comex_produtos.id_produto == 0) { ModelState.AddModelError("Model", "Campo [Produto/Part Number] é de preenchimento obrigatório"); };
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                if (ModelState.IsValid)
                {
                    view_record_gc_comex_produtos.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    view_record_gc_comex_produtos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;

                    if (view_record_gc_comex_produtos.id_produto > 0)
                    {
                        view_record_gc_comex_produtos.item_cadastro_novo = false;
                        g_produtos record_g_produtos = db.g_produtos.Find(view_record_gc_comex_produtos.id_produto);
                        if (record_g_produtos != null)
                        {
                            if (view_record_gc_comex_produtos.traducao.EmptyIfNull().ToString().Trim() != record_g_produtos.nome.EmptyIfNull().ToString().Trim())
                            {
                                view_record_gc_comex_produtos.item_cadastro_atualizar = true;
                            }
                            else
                            {
                                view_record_gc_comex_produtos.item_cadastro_atualizar = false;
                            }
                        }
                    }

                    view_record_gc_comex_produtos.datahora_alteracao = DataHoraAtual;
                    view_record_gc_comex_produtos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(view_record_gc_comex_produtos).State = EntityState.Modified;
                    try
                    {
                        db.SaveChanges();

                        // Log
                        String LogAlteracao = LibDB.CompareDataTable(record_old_gc_comex_produtos, view_record_gc_comex_produtos);
                        if (LogAlteracao.EmptyIfNull().ToString().Length > 0) 
                        {
                            if (view_record_gc_comex_produtos.id_produto > 0) 
                            { 
                                LogAlteracao = "Atualização Dados [" + LogAlteracao + "]"; } else { LogAlteracao = "Novo Produto Comex | " + LogAlteracao;
                                LibAudit.SaveAudit(db, true,"gc_comex_produtos", view_record_gc_comex_produtos.id_comex_produto, LogAlteracao);
                            }
                        };
                        msgRetorno += "Produto Comex ATUALIZADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                        sucesso = true;
                    }
                    catch (DbEntityValidationException ex)
                    {
                        return JsonAjaxErroValidacao(ex);
                    }
                    catch (Exception e)
                    {
                        return JsonAjaxErro(e);
                    }
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region FormProcessarProdutosPreNovos
        public ActionResult FormProcessarProdutosPreNovos(int? id)
        {
            String MsgInfo = String.Empty;
            int RowsLimit = 30;
            ViewBag.Title = "Processar Novos Produtos";
            String SqlComexProdutos = String.Empty;
            SqlComexProdutos = string.Empty;
            SqlComexProdutos += " select * from gc_comex_produtos where ativo = 1 and item_cadastro_novo = 1 ";
            if (CachePersister.userIdentity.IdVendedor > 0) { SqlComexProdutos += " and id_usuario_cadastro = " + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString() + " "; };
            List<gc_comex_produtos> ListaComexProdutos = db.gc_comex_produtos.SqlQuery(SqlComexProdutos).ToList();
            List<gc_comex_produtos> ListaComexProdutosVincular = new List<gc_comex_produtos>();
            List<g_produtos> ListaGDIProdutos = db.g_produtos.Where(p => p.ativo == true && p.importado == true).OrderBy(p => p.nome).ToList();

            try
            {
                foreach (var ComexProduto in ListaComexProdutos)
                {
                    String PNOficial = ComexProduto.pn.EmptyIfNull().ToString().EmptyIfNull().ToUpperInvariant();
                    String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                    String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                    String PNCuringaZERO = PNAuxiliar.Replace("O", "0");
                    g_produtos record_g_produtos = ListaGDIProdutos.Where(p => (p.codigo == ComexProduto.pn || p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO)).FirstOrDefault();
                    if (record_g_produtos != null) { ComexProduto.id_produto = record_g_produtos.id_produto; };
                    if (ComexProduto.traducao.EmptyIfNull().ToString().Length > 0) { ComexProduto.description = ComexProduto.traducao.EmptyIfNull().ToString(); };
                    ComexProduto.id_produto = -1;
                    ListaComexProdutosVincular.Add(ComexProduto);
                    if (ListaComexProdutosVincular.Count >= RowsLimit) 
                    {
                        MsgInfo = "<b>Atenção</b> Existem "+ ListaComexProdutos.Count.ToString() + " que atendem a essa situação!" + "</br>" + "É necessário realizar o processamento em blocos de 50 registros.";
                        break; 
                    };
                }
            }
            finally { }


            var comboProdutos = new List<SelectListItem>();
            try
            {
                int _SizeNomeItem = 100;
                int _DisplayScreenWidth = 0;
                int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = 50; }
                if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 400)) { _SizeNomeItem = 40; }
                if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 300)) { _SizeNomeItem = 30; }
                comboProdutos.Add(new SelectListItem { Value = "-1", Text = "[ SELECIONE O PRODUTO ]" });
                comboProdutos.Add(new SelectListItem { Value = "0", Text = "[ NOVO PRODUTO/PN ]" });
                foreach (g_produtos Produto in ListaGDIProdutos)
                {
                    String IdProduto = Produto.id_produto.EmptyIfNull().ToString().Trim();
                    String NomeProduto = Produto.nome.EmptyIfNull().ToString().Trim();
                    if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; };
                    comboProdutos.Add(new SelectListItem { Value = IdProduto, Text = NomeProduto });
                }
            }
            finally { }
            ViewBag.comboProdutos = comboProdutos;
            ViewBag.MsgInfo = MsgInfo;
            return View(ListaComexProdutosVincular);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxProcessarProdutosPreNovos(List<gc_comex_produtos> ListaComexProdutosVincular)
        {
            bool sucesso = false;
            int QtdErros = 0;
            int QtdProdutosVinculados = 0;
            int QtdProdutosCadastrados = 0;
            String msgRetorno = String.Empty;
            String msgErroTipo1 = String.Empty;
            String msgErroTipo2 = String.Empty;
            String resultadoProcessamento = String.Empty;
            String DescricaoProduto = String.Empty;
            String idProcessamentoGravado = "0";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            List<gc_comex_produtos> ListaComexProdutos = db.gc_comex_produtos.Where(p => p.ativo == true && p.item_cadastro_novo == true).ToList();
            List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true).ToList();
            try
            {
                foreach (var ComexProdutoNovo in ListaComexProdutosVincular)
                {
                    gc_comex_produtos ComexProduto = ListaComexProdutos.Where(c => c.id_comex_produto == ComexProdutoNovo.id_comex_produto).FirstOrDefault();

                    if (ComexProdutoNovo.id_produto < 0) 
                    { 
                        QtdErros += 1;
                        msgRetorno += "- PN [" + ComexProduto.pn.EmptyIfNull().ToString() + "] não vinculado!" + "<br/>";
                    }
                    else if (ComexProdutoNovo.id_produto > 0)
                    {
                        g_produtos RecordProdutoVerificar = ListaProdutosGDI.Where(p => p.id_produto == ComexProdutoNovo.id_produto).FirstOrDefault();

                        if (RecordProdutoVerificar.importado == false)
                        {
                            QtdErros += 1;
                            msgRetorno += "- Vínculo com produto temporário [" + RecordProdutoVerificar.codigo.EmptyIfNull().ToString().Trim() + "] não permitido!"+"<br/>";
                        }
                        else
                        {
                            String PNOficial = ComexProduto.pn.EmptyIfNull().ToString();
                            String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                            if ((PNOficial != RecordProdutoVerificar.codigo) && (PNAuxiliar != RecordProdutoVerificar.codigo_auxiliar) || (PNCuringaOH != RecordProdutoVerificar.codigo_variacao1) || (PNCuringaZERO != RecordProdutoVerificar.codigo_variacao2))
                            {
                                QtdErros += 1;
                                msgRetorno += "- PNs divergentes [" + PNOficial + " > " + RecordProdutoVerificar.codigo + "] não podem ser vinculados!" + "<br/>";
                            }
                        }
                    }
                }
                
                if (QtdErros == 0)
                {
                    foreach (var ProdutoVincularView in ListaComexProdutosVincular)
                    {
                        gc_comex_produtos ProdutoComexVincular = db.gc_comex_produtos.Find(ProdutoVincularView.id_comex_produto);
                        if (ProdutoVincularView.id_produto == 0) // Confirmar novamente se realmente se trata de um produto novo
                        {
                            // CADASTRO DE PRODUTOS - 4                            
                            String PNOficial = ProdutoComexVincular.pn.EmptyIfNull().ToString();
                            String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                            g_produtos ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                            try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar
                            if (ProdutoGDI != null)
                            {
                                ProdutoVincularView.id_produto = ProdutoGDI.id_produto;
                            }
                        };

                        if (ProdutoVincularView.id_produto == 0) 
                        {
                            // NOVO PRODUTO GDI - NEW G_PRODUTOS
                            g_produtos new_g_produto = new g_produtos();
                            new_g_produto.ativo = true;
                            new_g_produto.importado = false;
                            new_g_produto.id_produto_tipo = 1;
                            new_g_produto.id_produto_ncm = 0;
                            new_g_produto.id_icms_cst = 0;
                            new_g_produto.icms_isento_uf = false;
                            new_g_produto.is_servico = false;
                            new_g_produto.has_corecharge = true;
                            new_g_produto.codigo = ProdutoComexVincular.pn;
                            new_g_produto.codigo_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ProdutoComexVincular.pn);
                            new_g_produto.codigo_variacao1 = new_g_produto.codigo_auxiliar.Replace("0", "O");
                            new_g_produto.codigo_variacao2 = new_g_produto.codigo_auxiliar.Replace("O", "0");
                            if (ProdutoComexVincular.traducao.EmptyIfNull().ToString().Length > 0) { DescricaoProduto = ProdutoComexVincular.traducao.EmptyIfNull().ToString(); }
                            else { DescricaoProduto = ProdutoComexVincular.description; }
                            if (!DescricaoProduto.StartsWith("PN:")) { DescricaoProduto = "PN:" + ProdutoComexVincular.pn.EmptyIfNull().ToString().Replace("PN:", "") + " - " + DescricaoProduto; };
                            new_g_produto.nome = DescricaoProduto;
                            new_g_produto.descricao = DescricaoProduto;
                            new_g_produto.fob1_dollar = ProdutoComexVincular.fob1_dollar;
                            new_g_produto.fob1_id_importacao = ProdutoComexVincular.fob1_id_importacao;
                            new_g_produto.valor_base = 0;
                            new_g_produto.controla_estoque = true;
                            new_g_produto.id_unidade_medida_compra = 0;
                            new_g_produto.id_unidade_medida_venda = 0;
                            new_g_produto.fator_conversao = 1;
                            new_g_produto.id_produto_grupo = 1;
                            new_g_produto.item_venda = true;
                            new_g_produto.item_revenda = false;
                            new_g_produto.item_uso_consumo = false;
                            new_g_produto.item_regulado_anp = false;
                            new_g_produto.datahora_cadastro = DataHoraAtual;
                            new_g_produto.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            new_g_produto.id_coligada = 1;
                            new_g_produto.id_filial = 1;

                            // Cadastro de NCM
                            if (ProdutoComexVincular.ncm.EmptyIfNull().ToString().Trim().Length > 0)
                            {
                                g_produtos_ncm record_g_produtos_ncm = db.g_produtos_ncm.Where(n => n.codigo_ncm == ProdutoComexVincular.ncm).FirstOrDefault();
                                if (record_g_produtos_ncm == null) { record_g_produtos_ncm = GDI.LibGDI.CadastrarProdutoNCM(db, ProdutoComexVincular.ncm); };
                                new_g_produto.id_produto_ncm = record_g_produtos_ncm.id_produto_ncm;
                            }

                            // Cadastro de Unidade de Medida
                            if (ProdutoComexVincular.unidade_medida.EmptyIfNull().ToString().Trim().Length > 0)
                            {
                                g_unidade_medida record_g_unidade_medida = db.g_unidade_medida.Where(m => m.descricao == ProdutoComexVincular.unidade_medida).FirstOrDefault();
                                if (record_g_unidade_medida == null) { record_g_unidade_medida = GDI.LibGDI.CadastrarUnidadeMedida(db, ProdutoComexVincular.unidade_medida); };
                                new_g_produto.id_unidade_medida_compra = record_g_unidade_medida.id_unidade_medida;
                                new_g_produto.id_unidade_medida_venda = record_g_unidade_medida.id_unidade_medida;
                            }

                            if ((ProdutoComexVincular.traducao.EmptyIfNull().ToString().Length > 0) && (new_g_produto.id_produto_ncm > 0) && (new_g_produto.id_unidade_medida_compra > 0))
                            {
                                new_g_produto.importado = true;
                            }
                            else
                            {
                                new_g_produto.importado = false;
                            }

                            db.g_produtos.Add(new_g_produto);
                            db.SaveChanges();
                            QtdProdutosCadastrados += 1;

                            LibAudit.SaveAudit(db, true,"g_produtos", new_g_produto.id_produto, "Novo Produto ERP - Vinculação ao Produto Comex id: " + ProdutoComexVincular.id_comex_produto.EmptyIfNull().ToString());
                            ProdutoComexVincular.id_produto = new_g_produto.id_produto;
                            ProdutoComexVincular.item_cadastro_novo = false;
                            ProdutoComexVincular.datahora_alteracao = DataHoraAtual;
                            ProdutoComexVincular.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(ProdutoComexVincular).State = EntityState.Modified;
                            db.SaveChanges();
                            LibAudit.SaveAudit(db, true,"gc_comex_produtos", ProdutoComexVincular.id_comex_produto, "Vinculação ao Produto ERP id: " + new_g_produto.id_produto.ToString());
                            QtdProdutosVinculados += 1;
                        }
                        else if (ProdutoVincularView.id_produto > 0)
                        {
                            ProdutoComexVincular.id_produto = ProdutoVincularView.id_produto;
                            ProdutoComexVincular.item_cadastro_novo = false;
                            ProdutoComexVincular.datahora_alteracao = DataHoraAtual;
                            ProdutoComexVincular.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(ProdutoComexVincular).State = EntityState.Modified;
                            db.SaveChanges();
                            LibAudit.SaveAudit(db, true,"gc_comex_produtos", ProdutoComexVincular.id_comex_produto, "Vinculação ao Produto ERP id: " + ProdutoComexVincular.id_produto.ToString());
                            QtdProdutosVinculados += 1;

                            g_produtos ProdutoGDI = db.g_produtos.Find(ProdutoComexVincular.id_produto);
                            if (ProdutoGDI != null)
                            {
                                bool ProdutoGDIAtualizado = false;

                                // NCM
                                if (ProdutoGDI.id_produto_ncm == 0)
                                {
                                    g_produtos_ncm record_g_produtos_ncm = db.g_produtos_ncm.Where(n => n.codigo_ncm == ProdutoComexVincular.ncm).FirstOrDefault();
                                    if (record_g_produtos_ncm == null) { record_g_produtos_ncm = GDI.LibGDI.CadastrarProdutoNCM(db, ProdutoComexVincular.ncm); };
                                    ProdutoGDI.id_produto_ncm = record_g_produtos_ncm.id_produto_ncm;
                                    LibAudit.SaveAudit(db, true, "g_produtos", ProdutoGDI.id_produto, "NCM: " + record_g_produtos_ncm.codigo_ncm.EmptyIfNull().ToString());
                                    ProdutoGDIAtualizado = true;
                                }
                                if (ProdutoGDI.id_unidade_medida_compra == 0)
                                {
                                    g_unidade_medida record_g_unidade_medida = db.g_unidade_medida.Where(m => m.descricao == ProdutoComexVincular.unidade_medida).FirstOrDefault();
                                    if (record_g_unidade_medida == null) { record_g_unidade_medida = GDI.LibGDI.CadastrarUnidadeMedida(db, ProdutoComexVincular.unidade_medida); };
                                    ProdutoGDI.id_unidade_medida_compra = record_g_unidade_medida.id_unidade_medida;
                                    ProdutoGDI.id_unidade_medida_venda = record_g_unidade_medida.id_unidade_medida;
                                    LibAudit.SaveAudit(db, true, "g_produtos", ProdutoGDI.id_produto, "Unidade de Medida " + " > " + record_g_unidade_medida.descricao.EmptyIfNull().ToString());
                                    ProdutoGDIAtualizado = true;
                                }
                                if (ProdutoGDI.fob1_dollar < ProdutoComexVincular.fob1_dollar)
                                {
                                    ProdutoGDI.fob1_dollar = ProdutoComexVincular.fob1_dollar;
                                    ProdutoGDI.fob1_id_importacao = ProdutoComexVincular.fob1_id_importacao;
                                    LibAudit.SaveAudit(db, false, "gc_produtos", ProdutoGDI.id_produto, "Atualização Fob US$: " + ProdutoGDI.fob1_dollar.ToString("0.00000"));
                                    ProdutoGDIAtualizado = true;
                                }

                                if (ProdutoGDI.importado == false)
                                {
                                    if ((ProdutoGDI.id_produto_ncm > 0) && (ProdutoGDI.id_unidade_medida_compra > 0))
                                    {
                                        ProdutoGDI.importado = true;
                                        LibAudit.SaveAudit(db, true,"g_produtos", ProdutoGDI.id_produto, "Atualização do Status: Produto atualizado para Importado");
                                        ProdutoGDIAtualizado = true;
                                    }
                                }
                                if (ProdutoGDIAtualizado == true)
                                {
                                    ProdutoGDI.datahora_alteracao = DataHoraAtual;
                                    ProdutoGDI.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(ProdutoGDI).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                    db.SaveChanges();
                    sucesso = true;
                    if (sucesso == true)
                    {
                        msgRetorno += "Processamento realizado com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        msgRetorno += (QtdProdutosCadastrados).ToString() + " Novo Produtos Cadastrados!" + "<br/>";
                        msgRetorno += (QtdProdutosVinculados).ToString() + " Novos Produtos Vinculados!" + "<br/>";
                    }
                }
                else
                {
                    msgRetorno = "Processamento NÃO realizado!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "") + "<br/><br/>" + msgRetorno;
                }
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacaoIdProcessamento(ex, idProcessamentoGravado);
            }
            catch (Exception e)
            {
                return JsonAjaxErroIdProcessamento(e, idProcessamentoGravado);
            }
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region FormProcessarProdutosPreAtualizar
        public ActionResult FormProcessarProdutosPreAtualizar(int? id)
        {
            ViewBag.Title = "Atualizar Produtos";
            String SqlComexProdutos = string.Empty;
            SqlComexProdutos = string.Empty;
            SqlComexProdutos += " select * from gc_comex_produtos where ativo = 1 and item_cadastro_atualizar = 1 ";
            if (CachePersister.userIdentity.IdVendedor > 0) { SqlComexProdutos += " and id_usuario_cadastro = " + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString() + " "; };
            List<gc_comex_produtos> ListaComexProdutos = db.gc_comex_produtos.SqlQuery(SqlComexProdutos).ToList();
            List<gc_comex_produtos> ListaComexProdutosAtualizar = new List<gc_comex_produtos>();
            List<g_produtos> ListaGDIProdutos = db.g_produtos.Where(p => p.ativo == true).ToList();
            foreach (var ComexProduto in ListaComexProdutos)
            {
                g_produtos record_g_produtos = ListaGDIProdutos.Find(p => p.id_produto == ComexProduto.id_produto) ;
                if (record_g_produtos != null) 
                {
                    ComexProduto.tag1 = false;
                    ComexProduto.tag2 = false;
                    ComexProduto.tag3 = false;
                    ComexProduto.description = record_g_produtos.nome;
                    ListaComexProdutosAtualizar.Add(ComexProduto);
                };
            }
            return View(ListaComexProdutosAtualizar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxProcessarProdutosPreAtualizar(List<gc_comex_produtos> ListaComexProdutosAtualizar)
        {
            bool sucesso = false;
            int QtdProdutosAtualizados = 0;
            String msgRetorno = String.Empty;
            String msgErroTipo1 = String.Empty;
            String msgErroTipo2 = String.Empty;
            String resultadoProcessamento = String.Empty;
            String idProcessamentoGravado = "0";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_comex_produtos ProdutoComex = new gc_comex_produtos();
            g_produtos ProdutoGDI = new g_produtos();
            List<g_produtos> ListaProdutosGDIAtualizar = new List<g_produtos>();

            try
            {
                foreach (var ProdutoAtualizarView in ListaComexProdutosAtualizar)
                {
                    String LogAlteracao1 = string.Empty;
                    String LogAlteracao2 = string.Empty;
                    String LogAlteracao3 = string.Empty;
                    ProdutoComex = db.gc_comex_produtos.Find(ProdutoAtualizarView.id_comex_produto);
                    if (ProdutoAtualizarView.tag1 == true)
                    {
                        QtdProdutosAtualizados += 1;
                        ProdutoGDI = db.g_produtos.Find(ProdutoComex.id_produto);
                        LogAlteracao1 = "Tradução: " + ProdutoGDI.nome + " > " + ProdutoAtualizarView.traducao.EmptyIfNull().ToString();
                        ProdutoGDI.nome = ProdutoAtualizarView.traducao;
                        ProdutoGDI.descricao = ProdutoAtualizarView.traducao;
                        ProdutoGDI.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                        ProdutoGDI.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;

                        if (ProdutoGDI.importado == false)
                        {
                            if ((ProdutoGDI.id_produto_ncm > 0) && (ProdutoGDI.id_unidade_medida_venda > 0))
                            {
                                LogAlteracao2 = "Atualização de Status: Produto temporário > Produto importado";
                                ProdutoGDI.importado = true;
                            }
                        }

                        ListaProdutosGDIAtualizar.Add(ProdutoGDI);
                        if (LogAlteracao1.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, LogAlteracao1); };
                        if (LogAlteracao2.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, LogAlteracao2); };
                        if (LogAlteracao3.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, LogAlteracao3); };
                    }
                    ProdutoComex.item_cadastro_atualizar = false;
                    ProdutoComex.datahora_alteracao = DataHoraAtual;
                    ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(ProdutoComex).State = EntityState.Modified;
                }

                // Atualizar Produtos GDI
                foreach (var ProdutoProdutoGDIAtualizar in ListaProdutosGDIAtualizar)
                {
                    ProdutoProdutoGDIAtualizar.datahora_alteracao = DataHoraAtual;
                    ProdutoProdutoGDIAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(ProdutoProdutoGDIAtualizar).State = EntityState.Modified;
                }

                db.SaveChanges();
                sucesso = true;
                msgRetorno += "Processamento realizado com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                msgRetorno += (QtdProdutosAtualizados).ToString() + " Produtos com descrições atualizadas!" + "<br/>";
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacaoIdProcessamento(ex, idProcessamentoGravado);
            }
            catch (Exception e)
            {
                return JsonAjaxErroIdProcessamento(e, idProcessamentoGravado);
            }
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCancelarCadastroProdutos
        public ActionResult ModalCancelarCadastroProdutos(String id)
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-trash-can", "", "red", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cancelar Cadastro de Produtos</b>";
            gc_comex_produtos record_gc_comex_produtos = new Db.gc_comex_produtos();
            record_gc_comex_produtos.description = id;
            return View("ModalCancelarCadastroProdutos", record_gc_comex_produtos);
        }

        [HttpPost]
        public ActionResult AjaxModalCancelarCadastroProdutos(gc_comex_produtos view_gc_comex_produtos)
        {
            bool Sucesso = false;
            int QtdErros = 0;
            int QtdProdutosCancelados = 0;
            String msgRetorno = String.Empty;
            try
            {
                String[] ListaIds = null;
                try { ListaIds = view_gc_comex_produtos.description.EmptyIfNull().ToString().Split(','); } catch(Exception) { };
                if (ListaIds == null)
                {
                    QtdErros += 1;
                    msgRetorno += "Não foram localizados produtos a serem cancelados!";
                }
                else if (ListaIds.Count() == 0)
                {
                    QtdErros += 1;
                    msgRetorno += "Não foram localizados produtos a serem cancelados!";
                }
                else
                {
                    foreach (string IdProdutoComex in ListaIds)
                    {
                        if (IdProdutoComex.EmptyIfNull().ToString().Length > 0)
                        {
                            gc_comex_produtos edit_gc_comex_produtos = db.gc_comex_produtos.Find(int.Parse(IdProdutoComex));
                            if (edit_gc_comex_produtos != null)
                            {
                                edit_gc_comex_produtos.ativo = false;
                                edit_gc_comex_produtos.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                                edit_gc_comex_produtos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                LibAudit.SaveAudit(db, false, "gc_comex_produtos", edit_gc_comex_produtos.id_comex_produto, "Cancelamento do cadastro do produto");
                                db.Entry(edit_gc_comex_produtos).State = EntityState.Modified;
                                QtdProdutosCancelados += 1;
                                msgRetorno += "Produto Temporário Id [" + IdProdutoComex.EmptyIfNull().ToString().Trim() + "] cancelado com sucesso!" + "<br/>";
                            }
                            else
                            {
                                QtdErros += 1;
                                msgRetorno += "Produto Temporário Id [" + IdProdutoComex.EmptyIfNull().ToString().Trim() + "] não foi localizado!" + "<br/>";
                            }
                        }
                    }
                    db.SaveChanges();
                }
                if (QtdProdutosCancelados > 0) { Sucesso = true; } else { Sucesso = false; };
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalDesativarCadastroProduto
        public ActionResult ModalDesativarProdutoComex(String id)
        {
            int IdProdutoComex = 0;
            int.TryParse(id, out IdProdutoComex);
            ViewBag.Title = "Produto Comex - Desativar Cadastro";
            PreencherLookupsComexProdutosComId();
            if (IdProdutoComex <= 0)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Produto COMEX", null);
                ViewBag.Title = "Produto Comex - Desativar Cadastro — (não localizado)";
                return View(new gc_comex_produtos());
            }
            gc_comex_produtos RecordComexProduto = db.gc_comex_produtos.Find(IdProdutoComex);
            if (RecordComexProduto == null)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Produto COMEX", IdProdutoComex);
                ViewBag.Title = "Produto Comex - Desativar Cadastro — (não localizado)";
                return View(new gc_comex_produtos { id_comex_produto = IdProdutoComex });
            }
            return View(RecordComexProduto);
        }

        [HttpPost]
        public ActionResult ajaxDesativarProdutoComex(gc_comex_produtos view_comex_produtos)
        {
            int qtdErros = 0;
            bool sucesso = false;
            String msgRetorno = String.Empty;
            String TextoSQLAtualizacao = String.Empty;
            gc_comex_produtos ProdutoComexSubstituto = null;
            gc_comex_produtos ProdutoComexDesativado = null;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (view_comex_produtos.id_comex_produto <= 0)
                {
                    qtdErros += 1;
                    msgRetorno += "Produto Comex principal não localizado!" + "<br/>";
                }
                else
                {
                    ProdutoComexDesativado = db.gc_comex_produtos.Find(view_comex_produtos.id_comex_produto);
                    if (ProdutoComexDesativado.ativo == false)
                    {
                        qtdErros += 1;
                        msgRetorno += "Produto Comex principal está desativado!" + "<br/>";
                    }
                }

                if (view_comex_produtos.id_comex_produto_substituto <= 0)
                {
                    qtdErros += 1;
                    msgRetorno += "Produto Comex substituto não localizado!" + "<br/>";
                }
                else
                {
                    ProdutoComexSubstituto = db.gc_comex_produtos.Find(view_comex_produtos.id_comex_produto_substituto);
                    if (ProdutoComexSubstituto.ativo == false)
                    {
                        qtdErros += 1;
                        msgRetorno += "Comex Produto substituto está desativado!" + "<br/>";
                    }
                }
                if (qtdErros == 0)
                {
                    if (view_comex_produtos.id_comex_produto == view_comex_produtos.id_comex_produto_substituto)
                    {
                        qtdErros += 1;
                        msgRetorno += "Item principal e substituto não podem ser o mesmo item!" + "<br/>";
                    }
                }
                if (qtdErros == 0)
                {
                    if ((ProdutoComexDesativado.fob1_dollar > 0) && (ProdutoComexSubstituto.fob1_dollar == 0)) { ProdutoComexSubstituto.fob1_dollar = ProdutoComexDesativado.fob1_dollar; };
                    if ((ProdutoComexDesativado.fob2_dollar > 0) && (ProdutoComexSubstituto.fob2_dollar == 0)) { ProdutoComexSubstituto.fob2_dollar = ProdutoComexDesativado.fob2_dollar; };
                    if ((ProdutoComexDesativado.fob3_dollar > 0) && (ProdutoComexSubstituto.fob3_dollar == 0)) { ProdutoComexSubstituto.fob3_dollar = ProdutoComexDesativado.fob3_dollar; };

                    ProdutoComexDesativado.ativo = false;
                    ProdutoComexDesativado.id_comex_produto_substituto = view_comex_produtos.id_comex_produto_substituto;
                    ProdutoComexDesativado.datahora_alteracao = DataHoraAtual;
                    ProdutoComexDesativado.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(ProdutoComexDesativado).State = EntityState.Modified;
                    
                    db.SaveChanges();

                    // Atualizar Importacoes Itens
                    TextoSQLAtualizacao = " update gc_comex_importacoes_itens set id_comex_produto =  " + ProdutoComexSubstituto.id_comex_produto.EmptyIfNull().ToString() + " " +
                                          " where id_comex_produto = " + ProdutoComexDesativado.id_comex_produto.EmptyIfNull().ToString() + " " +
                                           " and id_importacao_item > 0";
                    int QtdItensImportacoes = LibDB.dbQueryExec(TextoSQLAtualizacao, db);

                    // Produtos Comex
                    TextoSQLAtualizacao = " update gc_comex_invoices_itens set id_comex_produto =  " + ProdutoComexSubstituto.id_comex_produto.EmptyIfNull().ToString() + " " +
                                          " where id_comex_produto = " + ProdutoComexDesativado.id_comex_produto.EmptyIfNull().ToString() + " " +
                                           " and id_invoice_item > 0";
                    int QtdItensInvoices = LibDB.dbQueryExec(TextoSQLAtualizacao, db);

                    sucesso = true;
                    msgRetorno += "Produto Comex substituído com sucesso!" + "<br/>";
                    if (QtdItensImportacoes > 0) { msgRetorno += QtdItensImportacoes.EmptyIfNull().ToString() + " Itens de Importações Atualizados!" + "<br/>"; };
                    if (QtdItensInvoices > 0) { msgRetorno += QtdItensInvoices.EmptyIfNull().ToString() + " Itens de Invoices Atualizados!" + "<br/>"; };
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

            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
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
        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroIdProcessamento(Exception ex, string idProcessamento)
        {
            return Json(new { success = false, msg = GdiMvcJsonResults.AjaxFailureMessage(ex), idProcessamento = idProcessamento ?? "0" }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacaoIdProcessamento(DbEntityValidationException ex, string idProcessamento)
        {
            return Json(new { success = false, msg = GdiMvcJsonResults.AjaxFailureValidationMessage(ex), idProcessamento = idProcessamento ?? "0" }, JsonRequestBehavior.AllowGet);
        }

    }
}