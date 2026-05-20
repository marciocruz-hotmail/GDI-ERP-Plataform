using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueInventario_*,gc_EstoqueInventario_Default")]
    public partial class EstoqueInventarioController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "gc_EstoqueInventario";
        public EstoqueInventarioController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueInventario_*,gc_EstoqueInventario_Actionread")]
        public ActionResult Index()
        {
            PreencherLookupsIndexInventario();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-warehouse", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Estoque > Inventário";
            return View();
        }

        #region GetDadosInventario
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueInventario_*,gc_EstoqueInventario_Default,gc_EstoqueInventario_Actionread")]
        public ActionResult GetDadosInventario(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            string errorMessage = "";
            string stackTrace = "";

            try
            {
                // ----------------------------
                // 1) Flags de filtro (mantido para UI)
                // ----------------------------
                bool filterDb = false;
                bool filterAdvanced = false;

                g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);

                if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) filterDb = true;
                else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) filterAdvanced = true;

                if (filterDb || filterAdvanced) filterOnOff = "1";

                // ----------------------------
                // 2) Filtro Local de Estoque
                // ----------------------------
                int idLocalEstoque = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idLocalEstoque);

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // ----------------------------
                // 3) Base query (LINQ, sem SQL string)
                // ----------------------------
                var query = db.gc_estoque_inventario
                    .AsNoTracking()
                    .Where(i => i.id_inventario > 0);

                if (idLocalEstoque > 0)
                {
                    filterOnOff = "1";
                    query = query.Where(i => i.id_local_estoque == idLocalEstoque);
                }

                // Se você ainda quiser aplicar filtro persistido no DB (sql_filtro) aqui,
                // só dá para fazer via SQL RAW/Expression Builder. Mantive somente o "flag" como estava.

                // ----------------------------
                // 4) Totais DataTables
                // ----------------------------
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // ----------------------------
                // 5) Ordenação (DataTables)
                // IMPORTANTE: OrderBy ANTES de Skip/Take (erro clássico do EF)
                // ----------------------------
                bool asc = (param.sSortDir_0 ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                int sortCol = param.iSortCol_0;

                // Default estável: id_inventario desc (igual ao seu SQL)
                IOrderedQueryable<Db.gc_estoque_inventario> ordered = query.OrderByDescending(i => i.id_inventario);

                // Se quiser respeitar ordenação do DataTables, ajuste conforme índices reais do seu grid.
                // Aqui deixei só a coluna 1 (id_inventario) como ordenável, coerente com seu orderingFunction anterior.
                if (param.iSortingCols > 0 && sortCol == 1)
                    ordered = asc ? query.OrderBy(i => i.id_inventario) : query.OrderByDescending(i => i.id_inventario);

                // ----------------------------
                // 6) Página (somente campos usados na view)
                // ----------------------------
                var page = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(i => new
                    {
                        i.id_inventario,
                        i.id_local_estoque,
                        i.aberto,
                        i.ajustes,

                        i.qtd_itens_inicial,
                        i.qtd_itens_incluidos,

                        i.id_usuario_abertura,
                        i.datahora_abertura,

                        i.id_usuario_finalizacao,
                        i.datahora_finalizacao
                    })
                    .ToList();

                // ----------------------------
                // 7) Carrega nomes de usuários (somente IDs da página)
                // (Evita N+1 do db.g_usuarios.Find dentro do foreach)
                // ----------------------------
                var idsUsuarios = page
                    .SelectMany(x => new[] { x.id_usuario_abertura, x.id_usuario_finalizacao })
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var usuarios = db.g_usuarios
                    .AsNoTracking()
                    .Where(u => idsUsuarios.Contains(u.id_usuario))
                    .Select(u => new { u.id_usuario, u.nome })
                    .ToList()
                    .ToDictionary(x => x.id_usuario, x => x.nome);

                // ----------------------------
                // 8) Métricas de itens (somente inventários ABERTOS da página)
                // (Evita 1 query por linha com DataTable)
                // ----------------------------
                var idsInventariosAbertos = page
                    .Where(x => x.aberto)
                    .Select(x => x.id_inventario)
                    .Distinct()
                    .ToList();

                var saldos = new Dictionary<int, (int conferidos, int correto, int maior, int menor)>();

                if (idsInventariosAbertos.Count > 0)
                {
                    var agg = db.gc_estoque_inventario_item
                        .AsNoTracking()
                        .Where(it => idsInventariosAbertos.Contains(it.id_inventario))
                        .GroupBy(it => it.id_inventario)
                        .Select(g => new
                        {
                            id_inventario = g.Key,
                            qtd_itens_conferidos = g.Sum(x => x.conferido ? 1 : 0),
                            qtd_itens_saldo_correto = g.Sum(x => (x.conferido && x.qtd_disponivel == x.qtd_disponivel_anterior) ? 1 : 0),
                            qtd_itens_saldo_maior = g.Sum(x => (x.conferido && x.qtd_disponivel > x.qtd_disponivel_anterior) ? 1 : 0),
                            qtd_itens_saldo_menor = g.Sum(x => (x.conferido && x.qtd_disponivel < x.qtd_disponivel_anterior) ? 1 : 0)
                        })
                        .ToList();

                    foreach (var a in agg)
                    {
                        saldos[a.id_inventario] = (a.qtd_itens_conferidos, a.qtd_itens_saldo_correto, a.qtd_itens_saldo_maior, a.qtd_itens_saldo_menor);
                    }
                }

                // ----------------------------
                // 9) Monta aaData
                // ----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var i in page)
                {
                    string localEstoque =
                        (i.id_local_estoque == 1) ? "Estoque GDI - BH" :
                        (i.id_local_estoque == 3) ? "Estoque GDI - SP" :
                        "";

                    string iconAberto = i.aberto
                        ? LibIcons.getIcon("fa-solid fa-pen-to-square", "Em Andamento", "orange", "fa-xl")
                        : LibIcons.getIcon("fa-regular fa-square-check", "Finalizado", "green", "fa-xl");

                    string iconAjustes = i.ajustes
                        ? LibIcons.getIcon("fa-solid fa-screwdriver-wrench", "Ajustes", "gray", "fa-xl")
                        : LibIcons.getIcon("fa-solid fa-clipboard-list", "Inventário", "gray", "fa-xl");

                    string dadosAbertura = "";
                    if (i.id_usuario_abertura > 0)
                    {
                        string nomeUser = usuarios.TryGetValue(i.id_usuario_abertura, out var nu) ? nu : "";
                        dadosAbertura = i.datahora_abertura.GetValueOrDefault().ToString("dd/MM/yy HH:mm") + "<br/>" + nomeUser;
                    }

                    string dadosFinalizacao = "";
                    if (i.id_usuario_finalizacao > 0)
                    {
                        string nomeUser = usuarios.TryGetValue(i.id_usuario_finalizacao, out var nu) ? nu : "";
                        dadosFinalizacao = i.datahora_finalizacao.GetValueOrDefault().ToString("dd/MM/yy HH:mm") + "<br/>" + nomeUser;
                    }

                    int qtdConferidos = 0, qtdCorreto = 0, qtdMaior = 0, qtdMenor = 0;
                    if (i.aberto && saldos.TryGetValue(i.id_inventario, out var s))
                    {
                        qtdConferidos = s.conferidos;
                        qtdCorreto = s.correto;
                        qtdMaior = s.maior;
                        qtdMenor = s.menor;
                    }

                    list.Add(new[]
                    {
                "", // Seleção
                i.id_inventario.ToString(),
                iconAberto,
                iconAjustes,
                localEstoque,

                i.qtd_itens_inicial.ToString(),
                qtdConferidos.ToString(),
                qtdCorreto.ToString(),
                qtdMaior.ToString(),
                qtdMenor.ToString(),

                i.qtd_itens_incluidos.ToString(),

                "0",
                "0",
                "0",
                "0",

                dadosAbertura,
                dadosFinalizacao,

                "" // Botão Editar
            });
                }

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
            catch (DbEntityValidationException ex)
            {
                errorMessage = LibExceptions.getDbEntityValidationException(ex);
                stackTrace = ex.ToString();
            }
            catch (WebException ex)
            {
                errorMessage = LibExceptions.getWebException(ex);
                stackTrace = ex.ToString();
            }
            catch (Exception ex)
            {
                errorMessage = LibExceptions.getExceptionShortMessage(ex);
                stackTrace = ex.ToString();
            }

            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = stackTrace, // em produção você pode retornar ""
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region GetDadosInventarioItem
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueInventario_*,gc_EstoqueInventario_Default,gc_EstoqueInventario_Actionread")]
        public ActionResult GetDadosInventarioItem(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string errorMessage = "";
            string stackTrace = "";
            string filterOnOff = "0";

            try
            {
                // ----------------------------
                // 1) Parse filtros (sem SQL string)
                // ----------------------------
                int idInventario = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idInventario);

                int idProduto = 0;
                int.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), out idProduto);

                int idArea = 0;
                int.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), out idArea);

                int idSecao = 0;
                int.TryParse(param.yesCustomField04.EmptyIfNull().ToString().Trim(), out idSecao);

                int idCorredor = 0;
                int.TryParse(param.yesCustomField05.EmptyIfNull().ToString().Trim(), out idCorredor);

                int idPrateleira = 0;
                int.TryParse(param.yesCustomField06.EmptyIfNull().ToString().Trim(), out idPrateleira);

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // Inventário é obrigatório pra essa tela
                if (idInventario <= 0)
                {
                    return Json(new
                    {
                        errorMessage = "Inventário inválido (id_inventario não informado).",
                        severity = "error",
                        stackTrace = "",
                        yesFilterOnOff = "0",
                        sEcho = param.sEcho,
                        iTotalRecords = 0,
                        iTotalDisplayRecords = 0,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                // Se algum filtro complementar estiver ativo, marca no UI
                if (idProduto > 0 || idArea > 0 || idSecao > 0 || idCorredor > 0 || idPrateleira > 0)
                    filterOnOff = "1";

                // ----------------------------
                // 2) Base query (EF / LINQ)
                // ----------------------------
                var query = db.gc_estoque_inventario_item
                    .AsNoTracking()
                    .Where(i => i.id_inventario_item > 0 && i.id_inventario == idInventario);

                if (idProduto > 0) query = query.Where(i => i.id_produto == idProduto);
                if (idArea > 0) query = query.Where(i => i.id_estoque_area == idArea);
                if (idSecao > 0) query = query.Where(i => i.id_estoque_secao == idSecao);
                if (idCorredor > 0) query = query.Where(i => i.id_estoque_corredor == idCorredor);
                if (idPrateleira > 0) query = query.Where(i => i.id_estoque_prateleira == idPrateleira);

                // Totais DataTables (contagem no banco)
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // ----------------------------
                // 3) Ordenação (OrderBy antes de Skip/Take)
                // ----------------------------
                bool asc = (param.sSortDir_0 ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                int sortCol = param.iSortCol_0;

                // Default: id_inventario_item desc (igual seu SQL)
                IOrderedQueryable<Db.gc_estoque_inventario_item> ordered = query.OrderByDescending(i => i.id_inventario_item);

                // Se quiser respeitar DataTables para coluna 1 (id_inventario_item)
                if (param.iSortingCols > 0 && sortCol == 1)
                    ordered = asc ? query.OrderBy(i => i.id_inventario_item) : query.OrderByDescending(i => i.id_inventario_item);

                // ----------------------------
                // 4) Página (campos mínimos)
                // ----------------------------
                var page = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(i => new
                    {
                        i.id_inventario_item,
                        i.id_produto,

                        i.id_estoque_area,
                        i.id_estoque_secao,
                        i.id_estoque_corredor,
                        i.id_estoque_prateleira,

                        i.qtd_disponivel_anterior,
                        i.qtd_disponivel,
                        i.qtd_disponivel_diferenca,
                        i.conferido
                    })
                    .ToList();

                // ----------------------------
                // 5) Produtos (somente IDs da página) via dataset existente
                //    Obs: dataset é em memória, mas carregamos 1x.
                // ----------------------------
                var dsProdutos = GetDatasetProdutosServicosLookup();
                var idsProdutosPagina = page.Select(x => x.id_produto).Distinct().ToList();

                // Monta dicionário id_produto => descricao_longa para lookup O(1)
                var mapProdutos = dsProdutos
                    .Where(p => idsProdutosPagina.Contains(p.id_produto_servico))
                    .GroupBy(p => p.id_produto_servico)
                    .ToDictionary(g => g.Key, g => g.First().descricao_longa.EmptyIfNull().ToString());

                // ----------------------------
                // 6) Monta aaData
                // ----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var i in page)
                {
                    string nomeProduto = mapProdutos.TryGetValue(i.id_produto, out var desc) ? desc : "";

                    // Endereço (mantive padrão atual: IDs)
                    string endereco = $"A {i.id_estoque_area} - S {i.id_estoque_secao} - C {i.id_estoque_corredor} - P {i.id_estoque_prateleira}";
                    nomeProduto = (nomeProduto ?? "") + "   [ " + endereco + " ]";

                    // Quantidades (anterior | atual) + ícone se conferido
                    string qtdAnterior = i.qtd_disponivel_anterior.ToString().Replace(",000", "").Replace(",00", "");
                    string qtdAtual = i.qtd_disponivel.ToString().Replace(",000", "").Replace(",00", "");

                    string qtdItem = qtdAnterior
                        + LibStringFormat.GetEspacesHtml(2) + "|" + LibStringFormat.GetEspacesHtml(2)
                        + qtdAtual;

                    if (i.conferido)
                    {
                        if (i.qtd_disponivel_diferenca == 0)
                            qtdItem += LibStringFormat.GetEspacesHtml(3) + LibIcons.getIcon("fa-solid fa-clipboard-check", "Item Conferido OK", "green", "");
                        else
                            qtdItem += LibStringFormat.GetEspacesHtml(3) + LibIcons.getIcon("fa-solid fa-clipboard-check", "Item Conferido com Divergência", "orange", "");
                    }

                    list.Add(new[]
                    {
                i.id_inventario_item.ToString(),
                nomeProduto,
                qtdItem,
                "" // Botão Editar
            });
                }

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
            catch (DbEntityValidationException ex)
            {
                errorMessage = LibExceptions.getDbEntityValidationException(ex);
                stackTrace = ex.ToString();
            }
            catch (WebException ex)
            {
                errorMessage = LibExceptions.getWebException(ex);
                stackTrace = ex.ToString();
            }
            catch (Exception ex)
            {
                errorMessage = LibExceptions.getExceptionShortMessage(ex);
                stackTrace = ex.ToString();
            }

            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = stackTrace, // em produção você pode retornar ""
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region FormInventarioItens
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueInventario_*,gc_EstoqueInventario_Default,gc_EstoqueInventario_Actionread")]
        public ActionResult FormInventarioItens(int? id)
        {
            String MsgBloqueio = String.Empty;
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            gc_estoque_inventario RecordInventario = db.gc_estoque_inventario.Find(id);
            if (RecordInventario == null)
            {
                return RedirectToAction("Index");
            }
            else
            {
                String TituloInventario = LibIcons.getIcon("fa-solid fa-warehouse", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Inventário > ";
                if (RecordInventario.id_local_estoque == 1) { TituloInventario += "Estoque GDI - BH > "; }
                else if (RecordInventario.id_local_estoque == 3) { TituloInventario += "Estoque GDI - SP > "; };
                TituloInventario += RecordInventario.datahora_abertura.GetValueOrDefault().ToString("dd/MM/yyyy");
                if (RecordInventario.aberto == false)
                {
                    MsgBloqueio = "Inventário já finalizado!";
                }
                else if (RecordInventario.processado == true)
                {
                    MsgBloqueio = "Inventário já processado!";
                }
                ViewBag.Title = TituloInventario;
                ViewBag.MsgBloqueio = MsgBloqueio;
                PreencherLookupsFormInventarioItens(RecordInventario.id_local_estoque);
                return View(RecordInventario);
            }
        }
        #endregion
        public ActionResult ModalCreateEditInventarioItem(int? id, int IdInventario)
        {
            String MsgBloqueio = string.Empty;
            int IdItem = 0;
            int.TryParse(id.EmptyIfNull().ToString(), out IdItem);
            gc_estoque_inventario RecordInventario = db.gc_estoque_inventario.Find(IdInventario);
            gc_estoque_inventario_item RecordInventarioItem = new gc_estoque_inventario_item();
            RecordInventarioItem.id_inventario = IdInventario;
            RecordInventarioItem.id_local_estoque = RecordInventario.id_local_estoque;
            if (IdItem == 0)
            {
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-dice-d6", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Inventário - Registrar Item</b>";
            }
            else
            {
                RecordInventarioItem = db.gc_estoque_inventario_item.Find(IdItem);
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-dice-d6", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Inventário - Ajustar Item</b>";
                if (RecordInventarioItem.processado == true)
                {
                    MsgBloqueio = "Item já foi processado anteriormente!";
                }
                RecordInventarioItem.qtd_disponivel = RecordInventarioItem.qtd_disponivel_anterior;
            }
            PreencherLookupsModalInventarioItem(RecordInventario.id_local_estoque);
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View("ModalCreateEditInventarioItem", RecordInventarioItem);
        }

        [HttpPost]
        public ActionResult AjaxCreateEditInventarioItem(gc_estoque_inventario_item view_gc_estoque_inventario_item)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            String msgRetorno = "";
            gc_estoque_inventario_item RecordInventarioItem = new gc_estoque_inventario_item();

            try
            {
                if (ModelState.IsValid)
                {
                    if ((view_gc_estoque_inventario_item.id_estoque_area < 0) || (view_gc_estoque_inventario_item.id_estoque_secao < 0) || (view_gc_estoque_inventario_item.id_estoque_corredor < 0) || (view_gc_estoque_inventario_item.id_estoque_prateleira < 0))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Informe os dados do [Endereço] do item!<br/>";
                    }
                    if (view_gc_estoque_inventario_item.qtd_disponivel < 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [Quantidade] não pode ser menor do que zero!<br/>";
                    }

                    if (view_gc_estoque_inventario_item.id_inventario_item == 0) // Inclusão de novo item de inventário
                    {
                        if (view_gc_estoque_inventario_item.id_produto <= 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Informe o Item!<br/>";
                        }
                        else
                        {
                            if (view_gc_estoque_inventario_item.id_inventario_item == 0)
                            {
                                gc_estoque_inventario_item RecordDuplicado = db.gc_estoque_inventario_item.Where(i => i.id_produto == view_gc_estoque_inventario_item.id_produto && i.id_inventario == view_gc_estoque_inventario_item.id_inventario).FirstOrDefault();
                                if (RecordDuplicado != null)
                                {
                                    qtdInconsistencias += 1;
                                    msgRetorno += " - Item já registrado para o inventário!<br/>";
                                }
                            }
                        }
                    }
                    else
                    {
                        RecordInventarioItem = db.gc_estoque_inventario_item.Find(view_gc_estoque_inventario_item.id_inventario_item);
                    }

                    if (((view_gc_estoque_inventario_item.qtd_disponivel != RecordInventarioItem.qtd_disponivel_anterior)) && (view_gc_estoque_inventario_item.obs.EmptyIfNull().ToString().Trim().Length <= 5))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Informe no campo [Obs] o MOTIVO da divergência do estoque!<br/>";
                    }

                    if (view_gc_estoque_inventario_item.obs.EmptyIfNull().ToString().Length > 100)
                    {
                        view_gc_estoque_inventario_item.obs = view_gc_estoque_inventario_item.obs.EmptyIfNull().ToString().Substring(0, 100);
                    }
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }

                if (qtdInconsistencias == 0)
                {
                    if (view_gc_estoque_inventario_item.id_inventario_item == 0)
                    {
                        RecordInventarioItem.id_inventario = view_gc_estoque_inventario_item.id_inventario;
                        RecordInventarioItem.id_local_estoque = view_gc_estoque_inventario_item.id_local_estoque;
                        RecordInventarioItem.id_produto = view_gc_estoque_inventario_item.id_produto;
                    }
                    RecordInventarioItem.qtd_disponivel = view_gc_estoque_inventario_item.qtd_disponivel;
                    RecordInventarioItem.qtd_disponivel_diferenca = RecordInventarioItem.qtd_disponivel - RecordInventarioItem.qtd_disponivel_anterior;
                    RecordInventarioItem.id_estoque_area = view_gc_estoque_inventario_item.id_estoque_area;
                    RecordInventarioItem.id_estoque_secao = view_gc_estoque_inventario_item.id_estoque_secao;
                    RecordInventarioItem.id_estoque_corredor = view_gc_estoque_inventario_item.id_estoque_corredor;
                    RecordInventarioItem.id_estoque_prateleira = view_gc_estoque_inventario_item.id_estoque_prateleira;
                    RecordInventarioItem.obs = view_gc_estoque_inventario_item.obs;
                    RecordInventarioItem.conferido = true;

                    if (view_gc_estoque_inventario_item.id_inventario_item > 0)
                    {
                        RecordInventarioItem.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        RecordInventarioItem.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                        db.Entry(RecordInventarioItem).State = EntityState.Modified;
                    }
                    else
                    {
                        RecordInventarioItem.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        RecordInventarioItem.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                        db.Entry(RecordInventarioItem).State = EntityState.Added;
                    }
                    db.SaveChanges();
                    sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        #region Modal Create Inventário
        public ActionResult ModalCreateInventario()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Abrir Novo Inventário</b>";
            gc_estoque_inventario NovoInventario = new gc_estoque_inventario();
            NovoInventario.id_local_estoque = -1;
            PreencherLookupsModalCreateInventario();
            return View(NovoInventario);
        }

        [HttpPost]
        public ActionResult AjaxCreateInventario(gc_estoque_inventario view_gc_estoque_inventario)
        {
            bool Sucesso = false;
            int QtdErros = 0;
            int QtdItensInventario = 0;
            String msgRetorno = String.Empty;
            gc_locais_estoque RecordLocalEstoque = null;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            List<g_produtos> ListaProdutosEstoque = new List<g_produtos>();
            try
            {
                if (view_gc_estoque_inventario.id_local_estoque <= 0)
                {
                    msgRetorno += "Campo <b>Local de Estoque</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                else
                {
                    RecordLocalEstoque = db.gc_locais_estoque.Find(view_gc_estoque_inventario.id_local_estoque);

                    if (RecordLocalEstoque != null)
                    {
                        if (RecordLocalEstoque.inventario_aberto == true)
                        {
                            msgRetorno += "Local de Estoque selecionado já está em inventário!<br/>";
                            QtdErros += 1;
                        }
                    }
                    else
                    {
                        msgRetorno += "Local de Estoque não encontrado!<br/>";
                        QtdErros += 1;
                    }
                }

                if (QtdErros == 0)
                {

                    gc_estoque_inventario NovoInventario = new gc_estoque_inventario();
                    NovoInventario.ajustes = view_gc_estoque_inventario.ajustes;
                    NovoInventario.aberto = true;
                    NovoInventario.processado = false;
                    NovoInventario.id_local_estoque = RecordLocalEstoque.id_local_estoque;
                    NovoInventario.id_usuario_abertura = CachePersister.userIdentity.IdUsuario;
                    NovoInventario.datahora_abertura = DataHoraAtual;
                    NovoInventario.qtd_itens_inicial = 0;
                    NovoInventario.qtd_itens_incluidos = 0;
                    NovoInventario.qtd_itens_conferidos = 0;
                    NovoInventario.qtd_itens_saldo_correto = 0;
                    NovoInventario.qtd_itens_saldo_maior = 0;
                    NovoInventario.qtd_itens_saldo_menor = 0;
                    db.Entry(NovoInventario).State = EntityState.Added;
                    db.SaveChanges();

                    if (view_gc_estoque_inventario.ajustes == false)
                    {
                        if (RecordLocalEstoque.id_local_estoque == 1) { ListaProdutosEstoque = db.g_produtos.Where(p => p.saldo_01_disponivel > 0).ToList(); }
                        else if (RecordLocalEstoque.id_local_estoque == 3) { ListaProdutosEstoque = db.g_produtos.Where(p => p.saldo_03_disponivel > 0).ToList(); }

                        foreach (g_produtos ProdutoEstoque in ListaProdutosEstoque)
                        {
                            gc_estoque_inventario_item NovoItemInventario = new gc_estoque_inventario_item();
                            NovoItemInventario.id_produto = ProdutoEstoque.id_produto;
                            NovoItemInventario.processado = false;
                            NovoItemInventario.id_inventario = NovoInventario.id_inventario;
                            NovoItemInventario.id_local_estoque = RecordLocalEstoque.id_local_estoque;

                            if (RecordLocalEstoque.id_local_estoque == 1)
                            {
                                NovoItemInventario.id_estoque_area = ProdutoEstoque.id_estoque01_area;
                                NovoItemInventario.id_estoque_secao = ProdutoEstoque.id_estoque01_secao;
                                NovoItemInventario.id_estoque_corredor = ProdutoEstoque.id_estoque01_corredor;
                                NovoItemInventario.id_estoque_prateleira = ProdutoEstoque.id_estoque01_prateleira;
                                NovoItemInventario.qtd_disponivel_anterior = ProdutoEstoque.saldo_01_disponivel;
                            }
                            else if (RecordLocalEstoque.id_local_estoque == 3)
                            {
                                NovoItemInventario.id_estoque_area = ProdutoEstoque.id_estoque03_area;
                                NovoItemInventario.id_estoque_secao = ProdutoEstoque.id_estoque03_secao;
                                NovoItemInventario.id_estoque_corredor = ProdutoEstoque.id_estoque03_corredor;
                                NovoItemInventario.id_estoque_prateleira = ProdutoEstoque.id_estoque03_prateleira;
                                NovoItemInventario.qtd_disponivel_anterior = ProdutoEstoque.saldo_03_disponivel;
                            }

                            db.Entry(NovoItemInventario).State = EntityState.Added;
                            QtdItensInventario += 1;
                        }

                        NovoInventario.qtd_itens_inicial = QtdItensInventario;
                        db.Entry(NovoInventario).State = EntityState.Modified;
                    }

                    RecordLocalEstoque.inventario_aberto = true;
                    db.Entry(RecordLocalEstoque).State = EntityState.Modified;
                    db.SaveChanges();

                    Sucesso = true;
                    msgRetorno = "Novo inventário ABERTO com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" + QtdItensInventario.ToString() + " Produtos a serem inventariados!";
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Modal Finalizar Inventário
        public ActionResult ModalFinalizarInventario(int IdInventario)
        {
            String MsgBloqueio = string.Empty;
            String MsgProcessamento = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_estoque_inventario RecordInventario = db.gc_estoque_inventario.Find(IdInventario);
            List<gc_estoque_inventario_item> ListaInventarioItens = new List<gc_estoque_inventario_item>();
            List<gc_estoque_inventario_item> ListaInventarioItensConferidos = new List<gc_estoque_inventario_item>();
            List<gc_estoque_inventario_item> ListaInventarioItensDivergentes = new List<gc_estoque_inventario_item>();
            String TitleModal = string.Empty;
            if (RecordInventario != null)
            {
                TitleModal = "Finalizar Inventário Nº " + RecordInventario.id_inventario.ToString();

                if (RecordInventario.aberto == false)
                {
                    MsgBloqueio = "Inventário já finalizado";
                    if (RecordInventario.datahora_finalizacao != null) { MsgBloqueio += " em " + RecordInventario.datahora_finalizacao.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm"); };
                    if (RecordInventario.id_usuario_finalizacao > 0)
                    {
                        g_usuarios RecordUsuario = db.g_usuarios.Find(RecordInventario.id_usuario_finalizacao);
                        if (RecordUsuario != null) { MsgBloqueio += " por " + RecordUsuario.nome.EmptyIfNull().ToString(); }
                    };
                    MsgBloqueio += "!";
                }
                else
                {
                    ListaInventarioItens = db.gc_estoque_inventario_item.Where(i => i.id_inventario == RecordInventario.id_inventario).ToList();
                    if (ListaInventarioItens.Count == 0)
                    {
                        MsgBloqueio = "Não há itens <b>A Processar<b> no inventário selecionado!";
                    }
                    else
                    {
                        ListaInventarioItensConferidos = ListaInventarioItens.Where(i => i.conferido == true).ToList();
                        ListaInventarioItensDivergentes = ListaInventarioItens.Where(i => i.qtd_disponivel_diferenca != 0).ToList();
                        MsgProcessamento += "<font color='#3b3939'><b>Confirmação do Processamento do Inventário<br/><br/>";
                        MsgProcessamento += ListaInventarioItens.Count.ToString() + LibStringFormat.GetTabHtml(1) + " Itens na lista do inventário!" + "<br/>";
                        MsgProcessamento += ListaInventarioItensConferidos.Count.ToString() + LibStringFormat.GetTabHtml(1) + " Itens conferidos!" + "<br/>";
                        MsgProcessamento += ListaInventarioItensDivergentes.Count.ToString() + LibStringFormat.GetTabHtml(1) + " Itens com estoque divergente!" + "<br/><br/>";
                        MsgProcessamento += "# # # # #   A T E N Ç Ã O   # # # # # " + "<br/>";
                        MsgProcessamento += "<b>Ao realizar o fechamento do inventário, o usuário confirma a verificação/validação de todas as informações contidas no inventário e se responsabiliza pelos ajustes gerenciais que serão realizados nos saldos de estoque e ficha de estoque dos itens que apresentaram quantidades divergentes<b/>!</font>";
                    }
                }

            }
            else
            {
                TitleModal = "Finalizar Inventário Nº " + RecordInventario.id_inventario.ToString();
                MsgBloqueio = "Inventário não localizado!";
            }
            ViewBag.Title = TitleModal;
            ViewBag.MsgBloqueio = MsgBloqueio;
            ViewBag.MsgProcessamento = MsgProcessamento;
            return View("ModalFinalizarInventario", RecordInventario);
        }

        [HttpPost]
        public ActionResult AjaxModalFinalizarInventario(gc_estoque_inventario ViewRecordInventario)
        {
            bool Sucesso = false;
            int QtdItensInicial = 0;
            int QtdItensIncluidos = 0;
            int QtdItensProcessados = 0;
            int QtdItensConferidos = 0;
            int QtdItenSaldoCorreto = 0;
            int QtdItenSaldoMaior = 0;
            int QtdItenSaldoMenor = 0;
            int QtdItensEnderecoDivergente = 0;
            int QtdInconsistenciasProcesso = 0;
            int QtdProdutosNaoLocalizados = 0;
            String MsgRetorno = "";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_estoque_inventario RecordInventario = null;
            List<gc_estoque_inventario_item> ListaInventarioItens = new List<gc_estoque_inventario_item>();
            List<gc_estoque_movimento> ListaInventarioMovimentosAtualizar = new List<gc_estoque_movimento>();
            List<g_produtos> ListaProdutos = new List<g_produtos>();
            List<g_produtos> ListaProdutosAtualizar = new List<g_produtos>();
            List<gc_estoque_inventario_item> ListaInventarioItensAtualizar = new List<gc_estoque_inventario_item>();
            try
            {
                RecordInventario = db.gc_estoque_inventario.Find(ViewRecordInventario.id_inventario);
                ListaProdutos = db.g_produtos.Where(p => p.ativo == true).ToList();

                if (RecordInventario == null)
                {
                    MsgRetorno = "Inventário não localizado!";
                    QtdInconsistenciasProcesso += 1;
                }
                else
                {
                    if (RecordInventario.aberto == false)
                    {
                        MsgRetorno = "Inventário já finalizado";
                        if (RecordInventario.datahora_finalizacao != null) { MsgRetorno += " em " + RecordInventario.datahora_finalizacao.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm"); };
                        if (RecordInventario.id_usuario_finalizacao > 0)
                        {
                            g_usuarios RecordUsuario = db.g_usuarios.Find(RecordInventario.id_usuario_finalizacao);
                            if (RecordUsuario != null) { MsgRetorno += " por " + RecordUsuario.nome.EmptyIfNull().ToString(); }
                        };
                        MsgRetorno += "!";
                        QtdInconsistenciasProcesso += 1;
                    }

                    if (QtdInconsistenciasProcesso == 0)
                    {
                        ListaInventarioItens = db.gc_estoque_inventario_item.Where(i => i.id_inventario == RecordInventario.id_inventario).ToList();
                        if (ListaInventarioItens.Count == 0)
                        {
                            MsgRetorno = "Não há itens <b>A Processar<b> no inventário selecionado!";
                            QtdInconsistenciasProcesso += 1;
                        }
                    }

                    if (QtdInconsistenciasProcesso == 0)
                    {
                        foreach (gc_estoque_inventario_item InventarioItem in ListaInventarioItens)
                        {
                            QtdItensProcessados += 1;
                            if (InventarioItem.conferido == true) { QtdItensConferidos += 1; };

                            bool IsProdutoAtualizado = false;
                            g_produtos RecordProduto = ListaProdutos.Where(p => p.id_produto == InventarioItem.id_produto).FirstOrDefault();
                            if (RecordProduto != null)
                            {
                                // Atualização do Endereço

                                if ((RecordProduto.id_estoque01_area != InventarioItem.id_estoque_area) || (RecordProduto.id_estoque01_secao != InventarioItem.id_estoque_secao) || (RecordProduto.id_estoque01_corredor != InventarioItem.id_estoque_corredor) || (RecordProduto.id_estoque01_prateleira != InventarioItem.id_estoque_prateleira))
                                {
                                    if (RecordInventario.id_local_estoque == 1)
                                    {
                                        RecordProduto.id_estoque01_area = InventarioItem.id_estoque_area;
                                        RecordProduto.id_estoque01_secao = InventarioItem.id_estoque_secao;
                                        RecordProduto.id_estoque01_corredor = InventarioItem.id_estoque_corredor;
                                        RecordProduto.id_estoque01_prateleira = InventarioItem.id_estoque_prateleira;
                                        QtdItensEnderecoDivergente += 1;

                                    }
                                    else if (RecordInventario.id_local_estoque == 3)
                                    {
                                        RecordProduto.id_estoque03_area = InventarioItem.id_estoque_area;
                                        RecordProduto.id_estoque03_secao = InventarioItem.id_estoque_secao;
                                        RecordProduto.id_estoque03_corredor = InventarioItem.id_estoque_corredor;
                                        RecordProduto.id_estoque03_prateleira = InventarioItem.id_estoque_prateleira;
                                        QtdItensEnderecoDivergente += 1;
                                    }
                                    IsProdutoAtualizado = true;
                                }


                                if ((InventarioItem.qtd_disponivel > InventarioItem.qtd_disponivel_anterior) || (InventarioItem.qtd_disponivel < InventarioItem.qtd_disponivel_anterior))
                                {
                                    InventarioItem.qtd_disponivel_diferenca = InventarioItem.qtd_disponivel - InventarioItem.qtd_disponivel_anterior;
                                    gc_estoque_movimento RecordEstoqueMovimento = new gc_estoque_movimento();

                                    RecordEstoqueMovimento.id_produto = InventarioItem.id_produto;
                                    RecordEstoqueMovimento.id_local_estoque = RecordInventario.id_local_estoque;
                                    RecordEstoqueMovimento.id_inventario = RecordInventario.id_inventario;
                                    RecordEstoqueMovimento.id_inventario_item = InventarioItem.id_inventario_item;

                                    if (InventarioItem.qtd_disponivel_diferenca > 0) // Ajustar estoque a maior
                                    {
                                        QtdItenSaldoMaior += 1;
                                        RecordEstoqueMovimento.id_estoque_movimento_tipo = 2;
                                        if (RecordInventario.id_local_estoque == 1) // O Que foi contado menos o que já estava lá
                                        {
                                            RecordEstoqueMovimento.qtd_disponivel = InventarioItem.qtd_disponivel_diferenca;
                                            RecordEstoqueMovimento.saldo_disponivel = RecordProduto.saldo_01_disponivel + InventarioItem.qtd_disponivel_diferenca;
                                        }
                                        else if (RecordInventario.id_local_estoque == 3) // O Que foi contato menos o que já estava lá
                                        {
                                            RecordEstoqueMovimento.qtd_disponivel = InventarioItem.qtd_disponivel_diferenca;
                                            RecordEstoqueMovimento.saldo_disponivel = RecordProduto.saldo_03_disponivel + InventarioItem.qtd_disponivel_diferenca;
                                        }
                                    }
                                    else if (InventarioItem.qtd_disponivel_diferenca < 0) // Ajustar estoque a menor
                                    {
                                        QtdItenSaldoMenor += 1;
                                        RecordEstoqueMovimento.id_estoque_movimento_tipo = 17;
                                        if (RecordInventario.id_local_estoque == 1) // O Que foi contado menos o que já estava lá
                                        {
                                            RecordEstoqueMovimento.qtd_disponivel = 0;
                                            RecordEstoqueMovimento.saldo_disponivel = RecordProduto.saldo_01_disponivel;
                                        }
                                        else if (RecordInventario.id_local_estoque == 3) // O Que foi contato menos o que já estava lá
                                        {
                                            RecordEstoqueMovimento.qtd_disponivel = 0;
                                            RecordEstoqueMovimento.saldo_disponivel = RecordProduto.saldo_03_disponivel;

                                        }
                                    }

                                    RecordEstoqueMovimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                    RecordEstoqueMovimento.datahora_cadastro = DataHoraAtual;
                                    ListaInventarioMovimentosAtualizar.Add(RecordEstoqueMovimento);

                                    if (InventarioItem.qtd_disponivel_diferenca > 0) // Ajustar estoque a maior
                                    {
                                        if (RecordInventario.id_local_estoque == 1) { RecordProduto.saldo_01_disponivel = RecordProduto.saldo_01_disponivel + InventarioItem.qtd_disponivel_diferenca; }
                                        else if (RecordInventario.id_local_estoque == 3) { RecordProduto.saldo_03_disponivel = RecordProduto.saldo_03_disponivel + InventarioItem.qtd_disponivel_diferenca; };
                                        IsProdutoAtualizado = true;
                                    }

                                    InventarioItem.processado = true;
                                    InventarioItem.id_usuario_processamento = CachePersister.userIdentity.IdUsuario;
                                    InventarioItem.datahora_processamento = DataHoraAtual;
                                    ListaInventarioItensAtualizar.Add(InventarioItem);

                                }
                                else if (InventarioItem.qtd_disponivel == InventarioItem.qtd_disponivel_anterior)
                                {
                                    QtdItenSaldoCorreto += 1;

                                }

                                if (IsProdutoAtualizado == true)
                                {
                                    ListaProdutosAtualizar.Add(RecordProduto);
                                }
                            }
                            else
                            {
                                QtdProdutosNaoLocalizados += 1;
                            }
                        }


                        if (QtdProdutosNaoLocalizados > 0)
                        {
                            MsgRetorno = "Foram identificados " + QtdProdutosNaoLocalizados.ToString() + " Não localizados no cadastro de produtos!";
                            QtdInconsistenciasProcesso += 1;
                        }
                        else
                        {
                            foreach (gc_estoque_movimento NewRecordEstoqueMovimento in ListaInventarioMovimentosAtualizar)
                            {
                                NewRecordEstoqueMovimento.datahora_cadastro = DataHoraAtual;
                                NewRecordEstoqueMovimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                db.Entry(NewRecordEstoqueMovimento).State = EntityState.Added;
                            }

                            foreach (g_produtos RecordProdutoAtualizar in ListaProdutosAtualizar)
                            {
                                RecordProdutoAtualizar.datahora_alteracao = DataHoraAtual;
                                RecordProdutoAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(RecordProdutoAtualizar).State = EntityState.Modified;
                            }

                            foreach (gc_estoque_inventario_item InventarioItemAtualizar in ListaInventarioItensAtualizar)
                            {
                                InventarioItemAtualizar.datahora_alteracao = DataHoraAtual;
                                InventarioItemAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(InventarioItemAtualizar).State = EntityState.Modified;
                            }

                            //RecordInventario.qtd_itens_inicial = 0; // Mantém o inicial
                            RecordInventario.qtd_itens_incluidos = QtdItensProcessados - RecordInventario.qtd_itens_inicial;
                            RecordInventario.qtd_itens_conferidos = QtdItensConferidos;
                            RecordInventario.qtd_itens_saldo_correto = 0;
                            RecordInventario.qtd_itens_saldo_maior = 0;
                            RecordInventario.qtd_itens_saldo_menor = 0;
                            RecordInventario.qtd_itens_saldo_correto = QtdItenSaldoCorreto;
                            RecordInventario.qtd_itens_saldo_maior = QtdItenSaldoMaior;
                            RecordInventario.qtd_itens_saldo_menor = QtdItenSaldoMenor;
                            RecordInventario.aberto = false;
                            RecordInventario.processado = true;
                            RecordInventario.id_usuario_finalizacao = CachePersister.userIdentity.IdUsuario;
                            RecordInventario.datahora_finalizacao = DataHoraAtual;
                            db.Entry(RecordInventario).State = EntityState.Modified;

                            gc_locais_estoque RecordLocalEstoque = db.gc_locais_estoque.Find(RecordInventario.id_local_estoque);
                            RecordLocalEstoque.inventario_aberto = false;
                            db.Entry(RecordLocalEstoque).State = EntityState.Modified;

                            db.SaveChanges();

                            MsgRetorno += "Inventário Nº <b> " + RecordInventario.id_inventario.ToString() + "</b> finalizado com sucesso" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                            MsgRetorno += "Qtd. Itens Inventário Inicial: " + LibStringFormat.GetTabHtml(1) + QtdItensInicial.ToString() + "<br/>";
                            MsgRetorno += "Qtd. Itens Incluídos: " + LibStringFormat.GetTabHtml(1) + QtdItensIncluidos.ToString() + "<br/>";
                            MsgRetorno += "Qtd. Itens Processados: " + LibStringFormat.GetTabHtml(1) + QtdItensProcessados.ToString() + "<br/>";
                            MsgRetorno += "Qtd. Itens Conferidos: " + LibStringFormat.GetTabHtml(1) + QtdItensConferidos.ToString() + "<br/>";
                            MsgRetorno += "Qtd. Itens Saldo Correto: " + LibStringFormat.GetTabHtml(1) + QtdItenSaldoCorreto.ToString() + "<br/>";
                            MsgRetorno += "Qtd. Itens Saldo Maior: " + LibStringFormat.GetTabHtml(1) + QtdItenSaldoMaior.ToString() + "<br/>";
                            MsgRetorno += "Qtd. Itens Saldo Menor: " + LibStringFormat.GetTabHtml(1) + QtdItenSaldoMenor.ToString() + "<br/>";
                            MsgRetorno += "Qtd. Itens Endereço Divergente: " + LibStringFormat.GetTabHtml(1) + QtdItensEnderecoDivergente.ToString() + "<br/>";
                            Sucesso = true;
                        }
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdInconsistenciasProcesso = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdInconsistenciasProcesso = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Relatorio Estoque Inventario
        [HttpPost]
        public ActionResult AjaxRelatorioEstoqueInventario(gc_estoque_inventario ViewRecordInventario)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            int QtdItensConferidos = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_estoque_inventario.xlsx");

            try
            {
                gc_estoque_inventario RecordInventario = db.gc_estoque_inventario.Find(ViewRecordInventario.id_inventario);
                List<gc_estoque_inventario_item> ListaInventarioItens = db.gc_estoque_inventario_item.Where(i => i.id_inventario == RecordInventario.id_inventario).ToList();
                List<g_usuarios> ListaUsuarios = db.g_usuarios.Where(i => i.ativo == true).ToList();
                var ListaProdutos = db.g_produtos.Select(p => new { p.id_produto, p.nome }).ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (ListaProdutos.Count > 0)
                {
                    foreach (gc_estoque_inventario_item RecordInventarioItem in ListaInventarioItens)
                    {
                        IndexLinha += 1;

                        String NomeProduto = string.Empty;
                        String EstoqueEndereco = string.Empty;
                        String NomeConferente = string.Empty;
                        var RecordProduto = ListaProdutos.Where(p => p.id_produto == RecordInventarioItem.id_produto).FirstOrDefault();
                        if (RecordProduto != null) { NomeProduto = RecordProduto.nome.EmptyIfNull().ToString().Trim(); };
                        EstoqueEndereco += "A " + RecordInventarioItem.id_estoque_area + " - S " + RecordInventarioItem.id_estoque_secao + " - C " + RecordInventarioItem.id_estoque_corredor + " - P " + RecordInventarioItem.id_estoque_prateleira;

                        WorkSheet.Cell(IndexLinha, 1).Value = RecordInventarioItem.id_inventario_item;
                        WorkSheet.Cell(IndexLinha, 2).Value = NomeProduto;
                        WorkSheet.Cell(IndexLinha, 3).Value = EstoqueEndereco;
                        WorkSheet.Cell(IndexLinha, 4).Value = RecordInventarioItem.qtd_disponivel_anterior;

                        if (RecordInventarioItem.conferido == true)
                        {
                            QtdItensConferidos += 1;
                            WorkSheet.Cell(IndexLinha, 5).Value = RecordInventarioItem.qtd_disponivel;
                            WorkSheet.Cell(IndexLinha, 6).Value = RecordInventarioItem.qtd_disponivel_diferenca;
                            if ((RecordInventarioItem.qtd_disponivel_diferenca > 0) || (RecordInventarioItem.qtd_disponivel_diferenca < 0)) { WorkSheet.Cell(IndexLinha, 6).Style.Fill.BackgroundColor = XLColor.Red; };
                            WorkSheet.Cell(IndexLinha, 7).Value = "Conferido";

                            g_usuarios RecordUsuario = ListaUsuarios.Where(u => u.id_usuario == RecordInventarioItem.id_usuario_cadastro).FirstOrDefault();
                            if (RecordUsuario != null) { NomeConferente = RecordUsuario.nome.EmptyIfNull().ToString() + " em "; };
                            NomeConferente += RecordInventarioItem.datahora_cadastro.GetValueOrDefault().ToString("dd/MM/yy HH:mm:ss");
                            WorkSheet.Cell(IndexLinha, 8).Value = NomeConferente;
                        }
                        NumeroRegistrosExportados += 1;
                    }

                    WorkSheet.Cell(1, 1).Value = "Relatório de Inventário de Estoque";
                    WorkSheet.Cell(2, 1).Value = "Inventário Id: " + RecordInventario.id_inventario.ToString() + " - Aberto em: " + RecordInventario.datahora_abertura.GetValueOrDefault().ToString("dd/MM/yy HH:mm:ss") + " por: " + ListaUsuarios.Where(u => u.id_usuario == RecordInventario.id_usuario_abertura).FirstOrDefault().nome.EmptyIfNull().ToString() + " - Qtd. Itens: " + ListaInventarioItens.ToString() + " Qtd. Conferidos: " + QtdItensConferidos.ToString();

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_Estoque_Inventário_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);

                    WorkSheet.Columns().AdjustToContents();
                    WorkBook.SaveAs(FileNameExportacao);
                    WorkBook.Dispose();

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 0; // 
                    record_g_processamento.id_processamento_modulo = 0; // 
                    record_g_processamento.detalhamento = "Relatório Estoque Inventário";
                    record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                    record_g_processamento.datahora_inicio = DataHoraAtual;
                    record_g_processamento.datahora_final = DataHoraAtual;
                    record_g_processamento.qtd_registros = NumeroRegistrosExportados;
                    record_g_processamento.qtd_reg_ok = NumeroRegistrosExportados;
                    record_g_processamento.qtd_reg_erro = 0;
                    record_g_processamento.processando = false;
                    record_g_processamento.concluido = true;
                    record_g_processamento.pathfile = FileNameExportacao;
                    record_g_processamento.id_coligada = 1;
                    record_g_processamento.id_filial = 1;
                    db.g_processamento.Add(record_g_processamento);
                    db.SaveChanges();

                    Sucesso = true;
                    IdProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                    MsgRetorno = "Relatório GERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "[" + NumeroRegistrosExportados.ToString() + " registros]" + "<br/><br/>" + "O Download do relatório será iniciado automaticamente!";
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

    }
}