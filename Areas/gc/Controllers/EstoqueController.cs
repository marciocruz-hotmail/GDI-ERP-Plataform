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
using GdiPlataform.Areas.gc.Services;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    public class EstoqueController : Controller
    {
        private GdiPlataformEntities db;
        public EstoqueController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region Index
        public ActionResult Index()
        {
            PreencherLookupsEstoque();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-boxes-stacked", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Posição de Estoque";
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosImportados(db);
            ViewBag.comboProdutosServicos.Insert(0, new SelectListItem { Value = "0", Text = "[ TODOS OS ITENS ]" });
            return View();
        }

        public void PreencherLookupsEstoque()
        {
            int _SizeNomeItem = 100;
            int _DisplayScreenWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
            if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = 50; }
            if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 400)) { _SizeNomeItem = 40; }
            if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 300)) { _SizeNomeItem = 30; }
            var comboProdutos = new List<SelectListItem>();
            try
            {
                comboProdutos.Add(new SelectListItem { Value = "0", Text = "[ TODOS OS PRODUTOS ]" });
                comboProdutos.Add(new SelectListItem { Value = "-1", Text = "[ PRODUTOS COM SALDO ]" });
                IQueryable<g_produtos> listaDbProdutos = db.g_produtos.Select(p => p).Where(p => p.ativo == true).OrderBy(p => p.nome);
                foreach (g_produtos item1 in listaDbProdutos)
                {
                    String IdProduto = item1.id_produto.EmptyIfNull().ToString().Trim();
                    String NomeProduto = item1.nome.EmptyIfNull().ToString().Trim();
                    if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; };
                    comboProdutos.Add(new SelectListItem { Value = IdProduto, Text = NomeProduto });
                }
            }
            finally { }
            ViewBag.comboProdutos = comboProdutos;
        }

        public ActionResult GetDadosEstoque(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            string errorMessage = "";
            string stackTrace = "";

            try
            {
                // ----------------------------
                // 1) Totais (BH/SP) - via LINQ (sem SQL string)
                // ----------------------------
                // Obs: se fob1_dollar puder ser null, ajuste para (p.fob1_dollar ?? 0)
                decimal valorEstoqueBH = db.g_produtos
                    .AsNoTracking()
                    .Where(p => p.saldo_01_disponivel > 0)
                    .Select(p => (decimal?)(p.saldo_01_disponivel * p.fob1_dollar))
                    .Sum() ?? 0m;

                decimal valorEstoqueSP = db.g_produtos
                    .AsNoTracking()
                    .Where(p => p.saldo_03_disponivel > 0)
                    .Select(p => (decimal?)(p.saldo_03_disponivel * p.fob1_dollar))
                    .Sum() ?? 0m;

                // ----------------------------
                // 2) Filtros
                // ----------------------------
                int idProduto = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idProduto);

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // Base query
                var query = db.g_produtos
                    .AsNoTracking()
                    .Where(p => p.id_produto > 0)
                    .Where(p => p.saldo_01_disponivel > 0 || p.saldo_03_disponivel > 0);

                if (idProduto > 0)
                {
                    filterOnOff = "1";
                    query = query.Where(p => p.id_produto == idProduto);
                }
                else
                {
                    // no seu original você ligava sempre "1" por ter sempre select com saldo >0
                    filterOnOff = "1";
                }

                // ----------------------------
                // 3) Totais DataTables
                // ----------------------------
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // ----------------------------
                // 4) Ordenação (DataTables) - precisa OrderBy antes do Skip no EF
                // ----------------------------
                bool asc = (param.sSortDir_0 ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                int sortCol = param.iSortCol_0;

                // Default estável
                IOrderedQueryable<Db.g_produtos> ordered = query.OrderBy(p => p.id_produto);

                // Seu código só considerava coluna 1 como id_produto.
                // Ajuste se quiser ordenar por outras colunas do grid.
                if (param.iSortingCols > 0 && sortCol == 1)
                    ordered = asc ? query.OrderBy(p => p.id_produto) : query.OrderByDescending(p => p.id_produto);
                else
                    ordered = query.OrderBy(p => p.id_produto);

                // ----------------------------
                // 5) Página (só campos necessários)
                // ----------------------------
                var page = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(p => new
                    {
                        p.id_produto,
                        p.nome,
                        p.fob1_dollar,
                        p.saldo_01_disponivel,
                        p.saldo_03_disponivel
                    })
                    .ToList();

                // ----------------------------
                // 6) Monta aaData
                // ----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var s in page)
                {
                    decimal fobDollar = s.fob1_dollar;
                    decimal fobEstoqueBH = s.saldo_01_disponivel * fobDollar;
                    decimal fobEstoqueSP = s.saldo_03_disponivel * fobDollar;

                    list.Add(new[]
                    {
                "", // Seleção
                s.id_produto.ToString(),
                s.nome.EmptyIfNull().ToString(),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", fobDollar).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                s.saldo_01_disponivel.ToString().Replace(",000",""),
                s.saldo_03_disponivel.ToString().Replace(",000",""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", fobEstoqueBH).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", fobEstoqueSP).Replace("R$ ", "").Replace("R$", "").Replace("$", "")
            });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesDisplayField01 = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorEstoqueBH).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                    yesDisplayField02 = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorEstoqueSP).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
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
                yesDisplayField01 = "Erro",
                yesDisplayField02 = "Erro",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalTransferenciaGerencial
        public ActionResult ModalTransferenciaGerencial()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-truck-arrow-right", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Transferência Gerencial - Entre Locais de Estoques</b>";
            gc_estoque_transferencia RecordEstoqueTransferencia = new gc_estoque_transferencia();
            RecordEstoqueTransferencia.id_local_estoque_origem = 0;
            RecordEstoqueTransferencia.id_local_estoque_destino = 0;
            RecordEstoqueTransferencia.id_produto = -1;
            ViewBag.comboLocaisEstoque = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosImportados(db);
            return View(RecordEstoqueTransferencia);
        }

        [HttpPost]
        public ActionResult AjaxModalTransferenciaGerencial(gc_estoque_transferencia RecordEstoqueTransferenciaView)
        {
            bool Sucesso = false;
            String MsgRetorno = String.Empty;
            try
            {
                /*EstoqueInventarioService ServicoTransferencia = new EstoqueInventarioService();
                TransferenciaRealizada = ServicoTransferencia.TransferenciaGerencial(RecordEstoqueTransferenciaView, db);
                if (TransferenciaRealizada == true) { Sucesso = true; }
                else { MsgRetorno += ServicoTransferencia.GetMsgProcessamento(); };*/
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
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Estoque - Recebimento Importação
        public ActionResult IndexRecebimentoImportacao()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-cart-flatbed", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Comex - Recebimento de Importação";
            return View();
        }
        public ActionResult GetDadosRecebimentoImportacao(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "1"; // aqui a listagem já nasce filtrada (importação + tipo=entrada + internalização + fechado)

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // Status NFe "ativa" (equivalente ao seu subselect)
                var idsNfeStatusAtivos = db.g_nfe_status.AsNoTracking()
                    .Where(s => s.nf_ativa)
                    .Select(s => s.id_nfe_status)
                    .Distinct()
                    .ToList();

                // -----------------------------------
                // Query base (sem SQL concatenado)
                //  - somente movimentos de importação (id_importacao > 0)
                //  - tipo entrada (t.tipo=1)
                //  - id_movimento_tipo = 7 (Internalização)
                //  - status = 2 (fechado)
                // -----------------------------------
                var baseQuery =
                    from m in db.gc_movimentos.AsNoTracking()
                    join t in db.gc_movimentos_tipos.AsNoTracking() on m.id_movimento_tipo equals t.id_movimento_tipo
                    where m.id_importacao > 0
                          && t.tipo == 1
                          && m.id_movimento_tipo == 7
                          && m.id_movimento_status == 2
                    select new
                    {
                        m.id_movimento,
                        m.id_importacao,
                        m.id_filial,
                        m.id_estoque_cd,
                        m.id_movimento_tipo,
                        m.id_movimento_status,

                        m.nf_numero,
                        m.nf_data_geracao,
                        m.datahora_cadastro,

                        m.qtd_itens,
                        m.valor_total_bruto,
                        m.icms_vicms,

                        m.receb_estoque_processado
                    };

                int totalRecords = baseQuery.Count();
                int totalDisplayRecords = totalRecords;

                var page = baseQuery
                    .OrderByDescending(x => x.id_movimento)
                    .Skip(start)
                    .Take(length)
                    .ToList();

                if (page.Count == 0)
                {
                    return Json(new
                    {
                        errorMessage = "",
                        stackTrace = "",
                        yesFilterOnOff = filterOnOff,
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = totalDisplayRecords,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                // -----------------------------------
                // Lookups SOMENTE para IDs da página
                // -----------------------------------
                var idsMov = page.Select(x => x.id_movimento).Distinct().ToList();
                var idsImp = page.Select(x => x.id_importacao).Where(x => x > 0).Distinct().ToList();
                var idsFil = page.Select(x => x.id_filial).Where(x => x > 0).Distinct().ToList();
                var idsCd = page.Select(x => x.id_estoque_cd).Where(x => x > 0).Distinct().ToList();

                var importacoesDict = db.gc_comex_importacoes.AsNoTracking()
                    .Where(i => idsImp.Contains(i.id_importacao))
                    .Select(i => new { i.id_importacao, i.numero })
                    .ToList()
                    .ToDictionary(x => x.id_importacao, x => x.numero);

                var filiaisDict = db.g_filiais.AsNoTracking()
                    .Where(f => idsFil.Contains(f.id_filial))
                    .Select(f => new { f.id_filial, f.nome })
                    .ToList()
                    .ToDictionary(x => x.id_filial, x => x.nome);

                var cdsDict = db.gc_estoque_cd.AsNoTracking()
                    .Where(cd => idsCd.Contains(cd.id_estoque_cd))
                    .Select(cd => new { cd.id_estoque_cd, cd.sigla })
                    .ToList()
                    .ToDictionary(x => x.id_estoque_cd, x => x.sigla);

                // Última NF por movimento (somente NFe status ativo)
                var lastNfByMov = db.gc_movimentos_nf.AsNoTracking()
                    .Where(nf => idsMov.Contains(nf.id_movimento) && idsNfeStatusAtivos.Contains(nf.id_nfe_status))
                    .Select(nf => new { nf.id_movimento, nf.id_movimento_nf, nf.nf_numero })
                    .ToList()
                    .GroupBy(x => x.id_movimento)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.id_movimento_nf).FirstOrDefault()
                    );

                // -----------------------------------
                // Montagem do aaData
                // -----------------------------------
                var list = new List<string[]>(page.Count);

                foreach (var m in page)
                {
                    // Importação
                    string nomeImportacao = importacoesDict.TryGetValue(m.id_importacao, out var impNum) ? (impNum ?? "").Trim() : "";

                    // Processado / A conferir
                    string recebimentoProcessado = m.receb_estoque_processado ? "1" : "0";
                    nomeImportacao += m.receb_estoque_processado
                        ? "<br/>Importação Processada"
                        : "<br/>Importação à Conferir/Receber";

                    // Número NF (campo do movimento + último registro em movimentos_nf)
                    string numeroNF = "";
                    if (!string.IsNullOrWhiteSpace(m.nf_numero) && m.nf_numero.Trim() != "0")
                        numeroNF = m.nf_numero.Trim();

                    string idMovimentoNF = "0";
                    if (lastNfByMov.TryGetValue(m.id_movimento, out var nfLast) && nfLast != null)
                    {
                        var nfNum = (nfLast.nf_numero ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(nfNum) && nfNum != "0")
                        {
                            idMovimentoNF = nfLast.id_movimento_nf.ToString();
                            if (numeroNF.Length > 0) numeroNF += " / ";
                            numeroNF += nfNum;
                        }
                    }

                    // Filial / CD
                    string filialNome = filiaisDict.TryGetValue(m.id_filial, out var fn) ? (fn ?? "") : "";
                    string cdSigla = cdsDict.TryGetValue(m.id_estoque_cd, out var cs) ? (cs ?? "") : "";

                    // Valor total (regra: tipo 7 soma VICMS)
                    decimal valorTotalNf = m.valor_total_bruto + m.icms_vicms;

                    // Data NF
                    DateTime dataNF = m.nf_data_geracao ?? m.datahora_cadastro;

                    list.Add(new[]
                    {
                "", // seleção
                m.id_movimento.ToString(),
                numeroNF,
                nomeImportacao,
                filialNome,
                cdSigla,
                dataNF.ToString("dd/MM/yy"),
                m.qtd_itens.ToString(),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalNf).Replace("R$ ","").Replace("R$","").Replace("$",""),
                idMovimentoNF,
                recebimentoProcessado,
                "", // Download PDF
                ""  // Receber Estoque
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
            catch (DbEntityValidationException e)
            {
                return Json(new
                {
                    errorMessage = LibExceptions.getDbEntityValidationException(e),
                    severity = "error",
                    stackTrace = e.ToString(),
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = 0,
                    iTotalDisplayRecords = 0,
                    aaData = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    errorMessage = LibExceptions.getExceptionShortMessage(e),
                    severity = "error",
                    stackTrace = e.ToString(),
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = 0,
                    iTotalDisplayRecords = 0,
                    aaData = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult FormRecebimentoItensImportacao(int? id)
        {
            String MsgBloqueio = String.Empty;
            String Titulo = String.Empty;
            String SubTitulo = String.Empty;
            if ((id == null) || (id == 0)) { return RedirectToAction("Index"); };
            
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            if (RecordMovimento == null) { return RedirectToAction("Index"); }
            else 
            {
                Titulo = LibIcons.getIcon("fa-solid fa-warehouse", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Recebimento Importação";
                if (RecordMovimento.receb_estoque_processado == true) { MsgBloqueio = "Recebimento já realizado anteriormente!"; };

                if (RecordMovimento.id_importacao > 0)
                {
                    gc_comex_importacoes RecordImportacao = db.gc_comex_importacoes.Find(RecordMovimento.id_importacao);
                    SubTitulo = RecordImportacao.numero.EmptyIfNull().ToString().Trim();
                }
                ViewBag.Title = Titulo;
                ViewBag.TituloAuxiliar = SubTitulo;
                ViewBag.MsgBloqueio = MsgBloqueio;
                ViewBag.comboOsCliente = GetLookupsRecebimentoItensImportacao(RecordMovimento.id_movimento); ;
                return View(RecordMovimento);
            }
        }
        public ActionResult GetDadosRecebimentoItensImportacao(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            string errorMessage = "";
            var list = new List<string[]>();

            try
            {
                // ----------------------------
                // Parâmetros
                // ----------------------------
                int idMovimento = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idMovimento);

                string filtroOS = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool filtrarPorOS = (!string.IsNullOrWhiteSpace(filtroOS) && filtroOS != "0" && filtroOS != "-1");

                if (idMovimento <= 0)
                {
                    return Json(new
                    {
                        errorMessage = "Movimento inválido.",
                        severity = "error",
                        stackTrace = "",
                        yesFilterOnOff = filterOnOff,
                        sEcho = param.sEcho,
                        iTotalRecords = 0,
                        iTotalDisplayRecords = 0,
                        aaData = list
                    }, JsonRequestBehavior.AllowGet);
                }

                // ----------------------------
                // Movimento (somente campos necessários)
                // ----------------------------
                var mov = db.gc_movimentos.AsNoTracking()
                    .Where(m => m.id_movimento == idMovimento)
                    .Select(m => new { m.id_movimento, m.id_importacao })
                    .FirstOrDefault();

                if (mov == null)
                {
                    return Json(new
                    {
                        errorMessage = "Movimento não encontrado.",
                        severity = "error",
                        stackTrace = "",
                        yesFilterOnOff = filterOnOff,
                        sEcho = param.sEcho,
                        iTotalRecords = 0,
                        iTotalDisplayRecords = 0,
                        aaData = list
                    }, JsonRequestBehavior.AllowGet);
                }

                // ----------------------------
                // Itens do movimento
                // ----------------------------
                var itensMov = db.gc_movimentos_itens.AsNoTracking()
                    .Where(i => i.id_movimento == mov.id_movimento)
                    .Select(i => new
                    {
                        i.id_movimento_item,
                        i.id_produto,
                        i.quantidade,
                        i.receb_import_recebido
                    })
                    .ToList();

                // Totais para DataTables (se quiser manter o total SEM filtro por OS, mantenha assim)
                int totalRecords = itensMov.Count;
                int totalDisplayRecords = totalRecords;

                // ----------------------------
                // Produtos (somente os usados nos itens)
                // ----------------------------
                var idsProdutos = itensMov.Select(i => i.id_produto).Distinct().ToList();

                var produtosDict = db.g_produtos.AsNoTracking()
                    .Where(p => idsProdutos.Contains(p.id_produto))
                    .Select(p => new { p.id_produto, p.nome })
                    .ToList()
                    .ToDictionary(x => x.id_produto, x => x.nome);

                // ----------------------------
                // Itens de invoices (somente para a importação)
                // + opcional: filtrar por OS no banco (evita buscar tudo)
                // ----------------------------
                var invQuery = db.gc_comex_invoices_itens.AsNoTracking()
                    .Where(x => x.ativo == true && x.id_importacao == mov.id_importacao);

                if (filtrarPorOS)
                {
                    invQuery = invQuery.Where(x => x.note == filtroOS);
                }

                var invItens = invQuery
                    .Select(x => new
                    {
                        x.id_produto,
                        x.item_qty,
                        x.customer,
                        x.note
                    })
                    .ToList();

                // Agrupa por produto para montar o bloco "OS / Cliente"
                var invByProduto = invItens
                    .GroupBy(x => x.id_produto)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Se filtrarPorOS, o total exibido deve refletir isso (senão, DataTables fica “mentindo”)
                if (filtrarPorOS)
                {
                    var idsProdutosComOS = new HashSet<int>(invByProduto.Keys);
                    totalDisplayRecords = itensMov.Count(i => idsProdutosComOS.Contains(i.id_produto));
                }

                // ----------------------------
                // Montagem aaData
                // ----------------------------
                foreach (var it in itensMov)
                {
                    // Se filtro por OS estiver ligado, ignora produtos sem ocorrências na OS
                    if (filtrarPorOS && !invByProduto.ContainsKey(it.id_produto))
                        continue;

                    string nomeProduto = produtosDict.TryGetValue(it.id_produto, out var np) ? (np ?? "") : "";

                    string statusItemConferido = it.receb_import_recebido ? "1" : "0";
                    string iconeStatus = it.receb_import_recebido
                        ? LibIcons.getIcon("fa-solid fa-check-to-slot", "Recebido", "green", "fa-xl")
                        : "";

                    // OS/Cliente (pode ter várias linhas)
                    string osCliente = "";
                    if (invByProduto.TryGetValue(it.id_produto, out var linhas))
                    {
                        foreach (var l in linhas)
                        {
                            string customer = (l.customer ?? "").Trim();
                            if (customer.ToUpperInvariant().StartsWith("GDI ")) customer = "GDI";

                            if (osCliente.Length > 0) osCliente += "<br/>";

                            string qtd = l.item_qty.ToString().Replace(",000", "").Replace(",00", "");
                            string os = (l.note ?? "").Trim();

                            osCliente += "[ " + qtd + " Un | " + customer + " | OS: " + os + " ]";
                        }
                    }

                    list.Add(new[]
                    {
                it.id_movimento_item.ToString(),
                nomeProduto,
                osCliente,
                it.quantidade.ToString().Replace(",000", "").Replace(",00", ""),
                statusItemConferido,
                iconeStatus,
                "" // botão editar
            });
                }

                return Json(new
                {
                    errorMessage = errorMessage,
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException e) { errorMessage = LibExceptions.getDbEntityValidationException(e); }
            catch (WebException e) { errorMessage = LibExceptions.getWebException(e); }
            catch (Exception e) { errorMessage = LibExceptions.getExceptionShortMessage(e); }

            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = list
            }, JsonRequestBehavior.AllowGet);
        }
        public List<SelectListItem> GetLookupsRecebimentoItensImportacao(int IdMovimento)
        {
            DataTable TableItensImportacao = null;
            List<DataRow> ListaItensEstoque = null;
            String SqlItensEstoque = string.Empty;
            String KeyOS = string.Empty;
            String ListaOS = string.Empty;
            var ComboOsCliente = new List<SelectListItem>();
            ComboOsCliente.Add(new SelectListItem { Value = "0", Text = "[ TODOS ]" });
            try
            {
                SqlItensEstoque += " select iteminvoice.id_invoice_item, item.id_movimento_item, item.id_produto, item.quantidade, ";
                SqlItensEstoque += " produto.nome, ";
                SqlItensEstoque += " movimento.id_importacao, ";
                SqlItensEstoque += " importacaoitem.id_comex_produto, ";
                SqlItensEstoque += " iteminvoice.note, iteminvoice.customer, iteminvoice.cd ";
                SqlItensEstoque += " from gc_movimentos_itens item ";
                SqlItensEstoque += " left join g_produtos produto on(produto.id_produto = item.id_produto) ";
                SqlItensEstoque += " left join gc_movimentos movimento on(movimento.id_movimento = item.id_movimento) ";
                SqlItensEstoque += " left join gc_comex_importacoes_itens importacaoitem on(importacaoitem.id_produto = item.id_produto and importacaoitem.id_importacao = movimento.id_importacao) ";
                SqlItensEstoque += " left join gc_comex_invoices_itens iteminvoice on(iteminvoice.id_comex_produto = importacaoitem.id_comex_produto and iteminvoice.id_importacao = movimento.id_importacao) ";
                SqlItensEstoque += " where item.id_movimento = " + IdMovimento.ToString() + " ";
                SqlItensEstoque += " order by iteminvoice.customer, iteminvoice.note; ";
                TableItensImportacao = LibDB.GetDataTable(SqlItensEstoque, db);
                ListaItensEstoque = TableItensImportacao.AsEnumerable().ToList();

                foreach (var RecordItemImportacao in ListaItensEstoque)
                {
                    String DadosClienteOS = String.Empty;
                    String Note = RecordItemImportacao["note"].EmptyIfNull().ToString().ToUpperInvariant().Trim();
                    String Customer = RecordItemImportacao["customer"].EmptyIfNull().ToString().ToUpperInvariant().Trim();
                    if (Customer.ToUpperInvariant().StartsWith("GDI ") == true) { Customer = "GDI AVIAÇÃO"; };
                    DadosClienteOS = Customer + "     ( OS: " + RecordItemImportacao["note"].EmptyIfNull().ToString().Trim() + " )";

                    KeyOS = Note;
                    if (Customer.ToString().Trim().Length > 10) { KeyOS += Customer.Substring(10); } else { KeyOS += Customer; }

                    if (ListaOS.IndexOf(KeyOS) < 0)
                    {
                        ComboOsCliente.Add(new SelectListItem { Value = Note, Text = DadosClienteOS });
                        ListaOS += KeyOS + ";";
                    }
                }
            }
            catch (Exception e) { throw (e); }
            return ComboOsCliente;
        }
        public ActionResult ModalConferenciaImportacaoItem(int? id)
        {
            String MsgBloqueio = string.Empty;
            String TituloAuxiliar = string.Empty;
            int IdMovimentoItem = 0;
            int.TryParse(id.EmptyIfNull().ToString(), out IdMovimentoItem);
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_movimentos_itens RecordMovimentoItem = db.gc_movimentos_itens.Find(IdMovimentoItem);
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(RecordMovimentoItem.id_movimento);
            g_produtos RecordProduto = db.g_produtos.Find(RecordMovimentoItem.id_produto);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-magnifying-glass", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Importação - Conferência Item</b>";
            if (RecordMovimentoItem.receb_import_processado == true) { MsgBloqueio = "Item já foi processado anteriormente!"; }

            CstPedidoConferenciaEntradaLote RecordCstPedidoConferenciaEntradaLote = new CstPedidoConferenciaEntradaLote();
            RecordCstPedidoConferenciaEntradaLote.id_movimento = RecordMovimentoItem.id_movimento;
            RecordCstPedidoConferenciaEntradaLote.id_movimento_item = RecordMovimentoItem.id_movimento_item;
            RecordCstPedidoConferenciaEntradaLote.item_nome = RecordMovimentoItem.produto_externo_nome.EmptyIfNull().ToString();
            RecordCstPedidoConferenciaEntradaLote.item_quantidade = RecordMovimentoItem.quantidade;

            RecordCstPedidoConferenciaEntradaLote.item_qtd_disponivel = RecordMovimentoItem.receb_import_qtd_disponivel;
            RecordCstPedidoConferenciaEntradaLote.item_qtd_falta = RecordMovimentoItem.receb_import_qtd_falta;
            RecordCstPedidoConferenciaEntradaLote.item_qtd_quarentena = RecordMovimentoItem.receb_import_qtd_quarentena;
            RecordCstPedidoConferenciaEntradaLote.item_peso_unit = RecordMovimentoItem.recebimento_peso_unit;
            RecordCstPedidoConferenciaEntradaLote.item_peso_total = RecordMovimentoItem.recebimento_peso_total;
            RecordCstPedidoConferenciaEntradaLote.item_obs = RecordMovimentoItem.receb_import_obs;

            for (int i = 1; i <= 50; i++)
            {
                string nomeId = $"id_estoque_lote_{i:D2}";
                string nomeQtd = $"lote{i:D2}_qtd";

                var propIdOrigem = RecordMovimentoItem.GetType().GetProperty(nomeId);
                var propIdDestino = RecordCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeId);

                if (propIdOrigem != null && propIdDestino != null)
                    propIdDestino.SetValue(RecordCstPedidoConferenciaEntradaLote,
                                           propIdOrigem.GetValue(RecordMovimentoItem));

                var propQtdOrigem = RecordMovimentoItem.GetType().GetProperty(nomeQtd);
                var propQtdDestino = RecordCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeQtd);

                if (propQtdOrigem != null && propQtdDestino != null)
                    propQtdDestino.SetValue(RecordCstPedidoConferenciaEntradaLote,
                                            propQtdOrigem.GetValue(RecordMovimentoItem));
            }


            if ((RecordProduto != null) && (RecordCstPedidoConferenciaEntradaLote.item_peso_unit == 0))
            {
                TituloAuxiliar = RecordProduto.nome.EmptyIfNull().ToString().Trim();
                if ((RecordMovimentoItem.recebimento_peso_unit <= 0) && (RecordProduto.peso > 0)) 
                {
                    RecordCstPedidoConferenciaEntradaLote.item_peso_unit = RecordProduto.peso;
                }
            }

            List<gc_estoque_lotes> ListaLotes = new List<gc_estoque_lotes>();
            ListaLotes = db.gc_estoque_lotes.Where(l => l.ativo == true && l.id_produto == RecordMovimentoItem.id_produto && l.id_importacao == RecordMovimento.id_importacao).OrderBy(l => l.codigo_lote).ToList();
            RecordCstPedidoConferenciaEntradaLote.ComboEstoqueLotes.Add(new SelectListItem { Value = "0", Text = "Selecione um lote" });
            foreach (var lote in ListaLotes)
            {
                String TextoLote = lote.codigo_lote.EmptyIfNull().ToString().Trim();
                if (lote.codigo_serial != null) { TextoLote += " | Serial.: " + lote.codigo_serial.EmptyIfNull().ToString(); }
                if (lote.data_validade != null) { TextoLote += " | Venc.: " + lote.data_validade.Value.ToString("dd/MM/yyyy"); }
                RecordCstPedidoConferenciaEntradaLote.ComboEstoqueLotes.Add(new SelectListItem { Value = lote.id_estoque_lote.EmptyIfNull().ToString(), Text = TextoLote.EmptyIfNull().ToString() });
            }

            if (TituloAuxiliar.EmptyIfNull().ToString().Trim().Length > 0) { if (TituloAuxiliar.EmptyIfNull().ToString().Trim().Length > 120) { TituloAuxiliar = TituloAuxiliar.Substring(0, 120) + "..."; }; };
            ViewBag.TituloAuxiliar = TituloAuxiliar;
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View("ModalConferenciaImportacaoItem", RecordCstPedidoConferenciaEntradaLote);
        }

        [HttpPost]
        public ActionResult AjaxConferenciaImportacaoItem(CstPedidoConferenciaEntradaLote ViewCstPedidoConferenciaEntradaLote)
        {
            bool Sucesso = false;
            int QtdInconsistencias = 0;
            String MsgRetorno = "";
            String NumerosSeriais = string.Empty;

            try
            {
                gc_movimentos_itens RecordMovimentoItem = new gc_movimentos_itens();
                RecordMovimentoItem = db.gc_movimentos_itens.Find(ViewCstPedidoConferenciaEntradaLote.id_movimento_item);

                if (RecordMovimentoItem == null)
                {
                    QtdInconsistencias += 1;
                    MsgRetorno += " - [Item] Não localizado!<br/>";
                }
                else if (ModelState.IsValid == true)
                {
                    if ((ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena > 0) && (ViewCstPedidoConferenciaEntradaLote.item_obs.EmptyIfNull().ToString().Trim().Length == 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Informe o MOTIVO/OBS dos itens de Não Conformidade!<br/>";
                    }
                    if ((ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel + ViewCstPedidoConferenciaEntradaLote.item_qtd_falta + ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena) != (RecordMovimentoItem.quantidade))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - A Soma das QUANTIDADES (OK + Falta + Não Conforme) deverá ser igual a quantidade do Item na NF!<br/>";
                    }

                    if ((ViewCstPedidoConferenciaEntradaLote.item_peso_unit <= 0) && (ViewCstPedidoConferenciaEntradaLote.item_peso_total <= 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Informe o PESO do item [Peso Unit.] ou [Peso Total]!<br/>";
                    }
                    else if ((ViewCstPedidoConferenciaEntradaLote.item_peso_unit > 0) && (ViewCstPedidoConferenciaEntradaLote.item_peso_total > 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Não é permitido informar [Peso Unit.] e [Peso Total] simultaneamente!<br/>";
                    }
                    if ((ViewCstPedidoConferenciaEntradaLote.item_peso_unit < 0) || (ViewCstPedidoConferenciaEntradaLote.item_peso_total < 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Não é permitido informar peso NEGATIVO para o item!<br/>";
                    }


                    // Conferido?
                    if (QtdInconsistencias == 0)
                    {
                        var tipo = ViewCstPedidoConferenciaEntradaLote.GetType();

                        for (int i = 1; i <= 50; i++)
                        {
                            string loteNum = i.ToString("D2");
                            string propId = $"id_estoque_lote_{loteNum}";
                            string propQtd = $"lote{loteNum}_qtd";
                            string propConferido = $"lote{loteNum}_conferido";

                            var idEstoqueLote = Convert.ToInt32(tipo.GetProperty(propId)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? 0);
                            var qtd = Convert.ToDecimal(tipo.GetProperty(propQtd)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? 0);
                            var conferido = Convert.ToBoolean(tipo.GetProperty(propConferido)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? false);

                            if (idEstoqueLote > 0 && (qtd == 0 || conferido == false))
                            {
                                QtdInconsistencias += 1;
                                MsgRetorno += $"Informe [Qtd] e [Conferido] para o Lote {loteNum}<br/>";
                            }
                        }
                    }


                    // Soma total
                    if (QtdInconsistencias == 0)
                    {
                        var tipo = ViewCstPedidoConferenciaEntradaLote.GetType();
                        decimal somaLotes = 0;

                        for (int i = 1; i <= 50; i++)
                        {
                            string loteNum = i.ToString("D2");
                            string propQtd = $"lote{loteNum}_qtd";

                            somaLotes += Convert.ToDecimal(tipo.GetProperty(propQtd)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? 0);
                        }

                        if (RecordMovimentoItem.quantidade != somaLotes)
                        {
                            QtdInconsistencias += 1;
                            MsgRetorno += "A Soma das quantidades informadas nos lotes separados deve ser igual a quantidade do item!<br/>";
                        }
                    }
                }
                else if (ModelState.IsValid == false)
                {
                    QtdInconsistencias += 1;
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                if (QtdInconsistencias == 0)
                {
                    RecordMovimentoItem.receb_import_recebido = true;
                    RecordMovimentoItem.receb_import_qtd_disponivel = ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel;
                    RecordMovimentoItem.receb_import_qtd_falta = ViewCstPedidoConferenciaEntradaLote.item_qtd_falta;
                    RecordMovimentoItem.receb_import_qtd_quarentena = ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena;
                    if (ViewCstPedidoConferenciaEntradaLote.item_peso_unit > 0)
                    {
                        RecordMovimentoItem.recebimento_peso_unit = ViewCstPedidoConferenciaEntradaLote.item_peso_unit;
                        RecordMovimentoItem.recebimento_peso_total = (ViewCstPedidoConferenciaEntradaLote.item_peso_unit * (ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel + ViewCstPedidoConferenciaEntradaLote.item_qtd_falta + ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena));
                    }
                    else if (ViewCstPedidoConferenciaEntradaLote.item_peso_total > 0)
                    {
                        RecordMovimentoItem.recebimento_peso_total = ViewCstPedidoConferenciaEntradaLote.item_peso_total;
                        RecordMovimentoItem.recebimento_peso_unit = (RecordMovimentoItem.recebimento_peso_total / ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel);
                    }
                    RecordMovimentoItem.receb_import_obs = ViewCstPedidoConferenciaEntradaLote.item_obs;


                    for (int i = 1; i <= 50; i++)
                    {
                        string nomeId = $"id_estoque_lote_{i:D2}";
                        string nomeQtd = $"lote{i:D2}_qtd";

                        var propIdOrigem = ViewCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeId);
                        var propIdDestino = RecordMovimentoItem.GetType().GetProperty(nomeId);

                        if (propIdOrigem != null && propIdDestino != null)
                            propIdDestino.SetValue(RecordMovimentoItem,
                                                   propIdOrigem.GetValue(ViewCstPedidoConferenciaEntradaLote));

                        var propQtdOrigem = ViewCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeQtd);
                        var propQtdDestino = RecordMovimentoItem.GetType().GetProperty(nomeQtd);

                        if (propQtdOrigem != null && propQtdDestino != null)
                            propQtdDestino.SetValue(RecordMovimentoItem,
                                                    propQtdOrigem.GetValue(ViewCstPedidoConferenciaEntradaLote));
                    }

                    RecordMovimentoItem.receb_import_datahora = LibDateTime.getDataHoraBrasilia();
                    RecordMovimentoItem.receb_import_id_usuario = CachePersister.userIdentity.IdUsuario;
                    RecordMovimentoItem.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    RecordMovimentoItem.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimentoItem).State = EntityState.Modified;

                    // Atualizar o peso do produto
                    g_produtos RecordProduto = db.g_produtos.Find(RecordMovimentoItem.id_produto);
                    if (RecordProduto != null) 
                    {
                        RecordProduto.peso = RecordMovimentoItem.recebimento_peso_unit;
                        db.Entry(RecordProduto).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxFinalizarRecebimentoImportacao(gc_movimentos view_g_movimentos)
        {
            int QtdErros = 0;
            int QtdItensNaoConferidos = 0;

            Decimal QtdItensTotal = 0;
            Decimal QtdItensConformes = 0;
            Decimal QtdItensNaoConformes = 0;
            Decimal QtdItensQuarentena = 0;
            Decimal QtdItensFaltantes = 0;

            bool Sucesso = false;
            String MsgRetorno = "";
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(view_g_movimentos.id_movimento);
            List<gc_movimentos_itens> ListaItensMovimento = new List<gc_movimentos_itens>();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                if (RecordMovimento == null)
                {
                    QtdErros += 1;
                    MsgRetorno = "Importação NÃO localizada no banco de dados!" + "<br/>";
                }
                else
                {
                    if (RecordMovimento.receb_estoque_processado == true)
                    {
                        QtdErros += 1;
                        MsgRetorno = "Importação já finalizada anteriormente!" + "<br/>";
                    }
                }

                ListaItensMovimento = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimento.id_movimento).ToList();
                foreach (var RecordItemImportacao in ListaItensMovimento)
                {
                    if (RecordItemImportacao.receb_import_recebido == false) { QtdItensNaoConferidos += 1; };
                    QtdItensTotal += RecordItemImportacao.receb_import_qtd_disponivel + RecordItemImportacao.receb_import_qtd_quarentena + RecordItemImportacao.receb_import_qtd_falta;
                    QtdItensConformes += RecordItemImportacao.receb_import_qtd_disponivel;
                    QtdItensNaoConformes += RecordItemImportacao.receb_import_qtd_quarentena + RecordItemImportacao.receb_import_qtd_falta;
                    QtdItensQuarentena += RecordItemImportacao.receb_import_qtd_quarentena;
                    QtdItensFaltantes += RecordItemImportacao.receb_import_qtd_falta;

                }

                if (QtdItensNaoConferidos > 0) 
                {
                    QtdErros += 1;
                    MsgRetorno = QtdItensNaoConferidos.ToString() + " Itens NÃO Conferidos!" + "<br/>"; 
                };

                if (QtdErros == 0)
                {
                    EstoqueInventarioService ServicoEstoqueBaixa = new EstoqueInventarioService();
                    bool EstoqueMovimentado = ServicoEstoqueBaixa.MovimentarEstoque(RecordMovimento.id_movimento, 6, db, true); // Entrada Importação
                    if (EstoqueMovimentado == false)
                    {
                        MsgRetorno += ServicoEstoqueBaixa.GetMsgProcessamento(); ;
                    }
                    else
                    {
                        RecordMovimento.receb_estoque_processado = true;
                        RecordMovimento.receb_estoque_id_usuario = CachePersister.userIdentity.IdUsuario;
                        RecordMovimento.receb_estoque_datahora = LibDateTime.getDataHoraBrasilia();
                        RecordMovimento.receb_estoque_qtd_itens_total = QtdItensTotal;
                        RecordMovimento.receb_estoque_qtd_itens_conformes = QtdItensConformes;
                        RecordMovimento.receb_estoque_qtd_itens_nc = QtdItensNaoConformes;
                        RecordMovimento.receb_estoque_qtd_itens_quarentena = QtdItensQuarentena;
                        RecordMovimento.receb_estoque_qtd_itens_falta = QtdItensFaltantes;
                        RecordMovimento.datahora_alteracao = DataHoraAtual;
                        RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordMovimento).State = EntityState.Modified;
                        db.SaveChanges();

                        Sucesso = true;
                        MsgRetorno = string.Empty;
                        MsgRetorno += "Recebimento de importação finalizado, e estoque atualizado com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        MsgRetorno += QtdItensTotal.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens TOTAL" + "<br/><br/>";
                        MsgRetorno += QtdItensConformes.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens VALIDADOS" + "<br/>";
                        MsgRetorno += QtdItensNaoConformes.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens NÃO CONFORMIDADE" + "<br/>";
                        MsgRetorno += QtdItensQuarentena.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens QUARENTENA" + "<br/>";
                        MsgRetorno += QtdItensFaltantes.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens FALTANTES" + "<br/>";
                    }
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

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Estoque - Recebimento de Estoque
        public ActionResult IndexRecebimentoEstoque()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-cart-flatbed", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Estoque - Recebimento de Estoque";
            return View();
        }
        public ActionResult GetDadosRecebimentoEstoque(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            string errorMessage = "";
            var list = new List<string[]>();

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // ----------------------------
                // Query base (sem SQL concatenado)
                // Regra original:
                // ((t.tipo = 1 and m.id_movimento_tipo in (10, 11, 18)) or (t.tipo = 3 and m.id_movimento_tipo = 18))
                // e m.id_movimento_status = 2
                // ----------------------------
                var baseQuery =
                    from m in db.gc_movimentos.AsNoTracking()
                    join t in db.gc_movimentos_tipos.AsNoTracking()
                        on m.id_movimento_tipo equals t.id_movimento_tipo into tJoin
                    from t in tJoin.DefaultIfEmpty()
                    where m.id_movimento_status == 2
                       && (
                            (t.tipo == 1 && (m.id_movimento_tipo == 10 || m.id_movimento_tipo == 11 || m.id_movimento_tipo == 18))
                         || (t.tipo == 3 && m.id_movimento_tipo == 18)
                          )
                    select new
                    {
                        // movimento
                        m.id_movimento,
                        m.id_cliente,
                        m.id_filial,
                        m.id_estoque_cd,
                        m.id_local_estoque, // mantive porque seu código usa (mesmo suspeito)
                        m.id_movimento_tipo,
                        m.nf_numero,
                        m.nf_s3_pdf,
                        m.nf_data_geracao,
                        m.datahora_cadastro,
                        m.qtd_itens,
                        m.valor_total_bruto,
                        m.icms_vicms,
                        m.receb_estoque_processado,

                        // tipo movimento
                        TipoCodigo = t.codigo
                    };

                int totalRecords = baseQuery.Count();
                int totalDisplayRecords = totalRecords;

                var page = baseQuery
                    .OrderByDescending(x => x.id_movimento)
                    .Skip(start)
                    .Take(length)
                    .ToList();

                // ----------------------------
                // Lookups (somente IDs da página)
                // ----------------------------
                var idsClientes = page.Select(x => x.id_cliente).Distinct().ToList();
                var idsFiliais = page.Select(x => x.id_filial).Distinct().ToList();
                var idsCds = page.Select(x => x.id_estoque_cd).Distinct().ToList();

                var clientesDict = db.g_clientes.AsNoTracking()
                    .Where(c => idsClientes.Contains(c.id_cliente))
                    .Select(c => new { c.id_cliente, c.nome })
                    .ToList()
                    .ToDictionary(x => x.id_cliente, x => x.nome);

                var filiaisDict = db.g_filiais.AsNoTracking()
                    .Where(f => idsFiliais.Contains(f.id_filial))
                    .Select(f => new { f.id_filial, f.nome })
                    .ToList()
                    .ToDictionary(x => x.id_filial, x => x.nome);

                var cdsDict = db.gc_estoque_cd.AsNoTracking()
                    .Where(cd => cd.ativo == true && idsCds.Contains(cd.id_estoque_cd))
                    .Select(cd => new { cd.id_estoque_cd, cd.sigla })
                    .ToList()
                    .ToDictionary(x => x.id_estoque_cd, x => x.sigla);

                // ----------------------------
                // DANFE Link para tipo 18/19 (busca em lote)
                // (no seu código você consulta gc_movimentos_nf dentro do loop: N+1)
                // ----------------------------
                var idsMovDanfe = page
                    .Where(x => x.nf_s3_pdf <= 0 && (x.id_movimento_tipo == 18 || x.id_movimento_tipo == 19))
                    .Select(x => x.id_movimento)
                    .Distinct()
                    .ToList();

                // pega a última NF autorizada (id_nfe_status == 8) por movimento
                var danfeByMov = new Dictionary<int, int>();
                if (idsMovDanfe.Count > 0)
                {
                    var nfAut = db.gc_movimentos_nf.AsNoTracking()
                        .Where(nf => idsMovDanfe.Contains(nf.id_movimento) && nf.id_nfe_status == 8)
                        .Select(nf => new { nf.id_movimento, nf.id_movimento_nf })
                        .ToList();

                    // se existir mais de uma, fica com a maior id_movimento_nf
                    danfeByMov = nfAut
                        .GroupBy(x => x.id_movimento)
                        .ToDictionary(g => g.Key, g => g.Max(v => v.id_movimento_nf));
                }

                // ----------------------------
                // Montagem aaData
                // ----------------------------
                foreach (var m in page)
                {
                    string numeroNF = (m.nf_numero ?? "").Trim();

                    // Cliente
                    string desc = "";
                    if (m.id_cliente > 0 && clientesDict.TryGetValue(m.id_cliente, out var nomeCli))
                        desc = (nomeCli ?? "").Trim();

                    // Situação / descrição por tipo
                    string recebProcessado = m.receb_estoque_processado ? "1" : "0";

                    if (m.id_movimento_tipo == 10)
                        desc += m.receb_estoque_processado
                            ? "<br/>NFe Entrada Nacional - Recebida/Conferida"
                            : "<br/>NFe Entrada Nacional - Recebimento/Conferência Pendente";
                    else if (m.id_movimento_tipo == 11)
                        desc += m.receb_estoque_processado
                            ? "<br/>NFe Devolução - Recebida/Conferida"
                            : "<br/>NFe Devolução - Recebimento/Conferência Pendente";
                    else if (m.id_movimento_tipo == 18)
                        desc += m.receb_estoque_processado
                            ? "<br/>NFe Transferência Entre Filiais - Recebida/Conferida"
                            : "<br/>NFe Transferência Entre Filiais - Recebimento/Conferência Pendente";

                    // Link DANFE
                    // regra original:
                    // - se nf_s3_pdf > 0 => "S3{nf_s3_pdf}"
                    // - senão, se tipo 18/19 => pegar gc_movimentos_nf id_nfe_status=8
                    string linkNFDanfe = "0";
                    if (m.nf_s3_pdf > 0)
                    {
                        linkNFDanfe = "S3" + m.nf_s3_pdf.ToString();
                    }
                    else if ((m.id_movimento_tipo == 18 || m.id_movimento_tipo == 19) && danfeByMov.TryGetValue(m.id_movimento, out var idMovNf))
                    {
                        linkNFDanfe = idMovNf.ToString();
                    }

                    // Filial / CD
                    string filialNome = (m.id_filial > 0 && filiaisDict.TryGetValue(m.id_filial, out var fn)) ? (fn ?? "") : "";
                    string cdSigla = (m.id_estoque_cd > 0 && cdsDict.TryGetValue(m.id_estoque_cd, out var cs)) ? (cs ?? "") : "";

                    // Valor NF
                    decimal valorTotalNf = m.valor_total_bruto;
                    // seu código só somava ICMS se tipo == 7 (aqui normalmente não é 7, mas mantive a regra)
                    if (m.id_movimento_tipo == 7) valorTotalNf += m.icms_vicms;

                    // Data NF
                    DateTime dataNF = m.datahora_cadastro;
                    if (m.nf_data_geracao != null) dataNF = m.nf_data_geracao.Value;

                    list.Add(new[]
                    {
                "", // seleção
                m.id_movimento.ToString(),
                numeroNF,
                desc,
                filialNome,
                cdSigla,
                dataNF.ToString("dd/MM/yy"),
                m.qtd_itens.ToString(),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalNf).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                linkNFDanfe,
                recebProcessado,
                "", // botão download
                ""  // botão receber estoque
            });
                }

                return Json(new
                {
                    errorMessage = errorMessage,
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException e) { errorMessage = LibExceptions.getDbEntityValidationException(e); }
            catch (WebException e) { errorMessage = LibExceptions.getWebException(e); }
            catch (Exception e) { errorMessage = LibExceptions.getExceptionShortMessage(e); }

            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = list
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult FormRecebimentoItensEstoque(int? id)
        {
            String MsgBloqueio = String.Empty;
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            if (RecordMovimento == null)
            {
                return RedirectToAction("Index");
            }
            else
            {
                String TituloRecebimento = LibIcons.getIcon("fa-solid fa-warehouse", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Estoque - Recebimento de Itens de Estoque";
                if (RecordMovimento.receb_estoque_processado == true)
                {
                    MsgBloqueio = "Recebimento já realizado anteriormente!";
                }
                ViewBag.Title = TituloRecebimento;
                ViewBag.MsgBloqueio = MsgBloqueio;
                return View(RecordMovimento);
            }
        }

        public ActionResult GetDadosRecebimentoItensEstoque(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            string errorMessage = "";
            var list = new List<string[]>();

            try
            {
                // ----------------------------
                // Parse IdMovimento (sem string solta)
                // ----------------------------
                int idMovimento = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idMovimento);

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // ----------------------------
                // Query (LINQ parametrizado, sem DataTable/SQL string)
                // ----------------------------
                var baseQuery =
                    from item in db.gc_movimentos_itens.AsNoTracking()
                    join prod in db.g_produtos.AsNoTracking()
                        on item.id_produto equals prod.id_produto into prodJoin
                    from prod in prodJoin.DefaultIfEmpty()
                    where item.id_movimento == idMovimento
                    orderby item.id_produto
                    select new
                    {
                        item.id_movimento_item,
                        item.quantidade,
                        NomeProduto = prod != null ? prod.nome : "",
                        item.receb_import_recebido
                    };

                int totalRecords = baseQuery.Count();
                int totalDisplayRecords = totalRecords;

                var page = baseQuery
                    .Skip(start)
                    .Take(length)
                    .ToList();

                // ----------------------------
                // Monta aaData
                // ----------------------------
                foreach (var r in page)
                {
                    bool recebido = r.receb_import_recebido;
                    string statusItemConferido = recebido ? "1" : "0";
                    string iconeStatus = recebido
                        ? LibIcons.getIcon("fa-solid fa-check-to-slot", "Recebido", "green", "fa-xl")
                        : string.Empty;

                    // Mantive sua formatação: remove ,000 e ,00
                    string qtd = r.quantidade.EmptyIfNull().ToString()
                        .Replace(",000", "")
                        .Replace(",00", "");

                    list.Add(new[]
                    {
                r.id_movimento_item.ToString(),
                (r.NomeProduto ?? "").Trim(),
                qtd,
                statusItemConferido,
                iconeStatus,
                "" // Botão Editar
            });
                }

                return Json(new
                {
                    errorMessage = errorMessage,
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException e) { errorMessage = LibExceptions.getDbEntityValidationException(e); }
            catch (WebException e) { errorMessage = LibExceptions.getWebException(e); }
            catch (Exception e) { errorMessage = LibExceptions.getExceptionShortMessage(e); }

            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = list
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModalConferenciaEstoqueItem(int? id)
        {
            /*String MsgBloqueio = string.Empty;
            String NomeProduto = string.Empty;
            int IdMovimentoItem = 0;
            int.TryParse(id.EmptyIfNull().ToString(), out IdMovimentoItem);
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_movimentos_itens RecordMovimentoItem = db.gc_movimentos_itens.Find(IdMovimentoItem);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-magnifying-glass", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Estoque - Conferência Item</b>";
            if (RecordMovimentoItem.receb_import_processado == true) { MsgBloqueio = "Item já foi processado anteriormente!"; }
            g_produtos RecordProduto = db.g_produtos.Find(RecordMovimentoItem.id_produto);
            ViewBag.TituloAuxiliar = RecordProduto.nome.EmptyIfNull().ToString();
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View("ModalConferenciaEstoqueItem", RecordMovimentoItem);*/

            String MsgBloqueio = string.Empty;
            String TituloAuxiliar = string.Empty;
            int IdMovimentoItem = 0;
            int.TryParse(id.EmptyIfNull().ToString(), out IdMovimentoItem);
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_movimentos_itens RecordMovimentoItem = db.gc_movimentos_itens.Find(IdMovimentoItem);
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(RecordMovimentoItem.id_movimento);
            g_produtos RecordProduto = db.g_produtos.Find(RecordMovimentoItem.id_produto);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-magnifying-glass", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Estoque - Conferência Item</b>";
            if (RecordMovimentoItem.receb_import_processado == true) { MsgBloqueio = "Item já foi processado anteriormente!"; }

            CstPedidoConferenciaEntradaLote RecordCstPedidoConferenciaEntradaLote = new CstPedidoConferenciaEntradaLote();
            RecordCstPedidoConferenciaEntradaLote.id_movimento = RecordMovimentoItem.id_movimento;
            RecordCstPedidoConferenciaEntradaLote.id_movimento_item = RecordMovimentoItem.id_movimento_item;
            RecordCstPedidoConferenciaEntradaLote.item_nome = RecordMovimentoItem.produto_externo_nome.EmptyIfNull().ToString();
            RecordCstPedidoConferenciaEntradaLote.item_quantidade = RecordMovimentoItem.quantidade;

            RecordCstPedidoConferenciaEntradaLote.item_qtd_disponivel = RecordMovimentoItem.receb_import_qtd_disponivel;
            RecordCstPedidoConferenciaEntradaLote.item_qtd_falta = RecordMovimentoItem.receb_import_qtd_falta;
            RecordCstPedidoConferenciaEntradaLote.item_qtd_quarentena = RecordMovimentoItem.receb_import_qtd_quarentena;
            RecordCstPedidoConferenciaEntradaLote.item_peso_unit = RecordMovimentoItem.recebimento_peso_unit;
            RecordCstPedidoConferenciaEntradaLote.item_peso_total = RecordMovimentoItem.recebimento_peso_total;
            RecordCstPedidoConferenciaEntradaLote.item_obs = RecordMovimentoItem.receb_import_obs;

            for (int i = 1; i <= 50; i++)
            {
                string nomeId = $"id_estoque_lote_{i:D2}";
                string nomeQtd = $"lote{i:D2}_qtd";

                var propIdOrigem = RecordMovimentoItem.GetType().GetProperty(nomeId);
                var propIdDestino = RecordCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeId);

                if (propIdOrigem != null && propIdDestino != null)
                    propIdDestino.SetValue(RecordCstPedidoConferenciaEntradaLote,
                                           propIdOrigem.GetValue(RecordMovimentoItem));

                var propQtdOrigem = RecordMovimentoItem.GetType().GetProperty(nomeQtd);
                var propQtdDestino = RecordCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeQtd);

                if (propQtdOrigem != null && propQtdDestino != null)
                    propQtdDestino.SetValue(RecordCstPedidoConferenciaEntradaLote,
                                            propQtdOrigem.GetValue(RecordMovimentoItem));
            }

            if ((RecordProduto != null) && (RecordCstPedidoConferenciaEntradaLote.item_peso_unit == 0))
            {
                TituloAuxiliar = RecordProduto.nome.EmptyIfNull().ToString().Trim();
                if ((RecordMovimentoItem.recebimento_peso_unit <= 0) && (RecordProduto.peso > 0))
                {
                    RecordCstPedidoConferenciaEntradaLote.item_peso_unit = RecordProduto.peso;
                }
            }

            List<gc_estoque_lotes> ListaLotes = new List<gc_estoque_lotes>();

            /*if (RecordMovimento.id_importacao > 0) { ListaLotes = db.gc_estoque_lotes.Where(l => l.ativo == true && l.id_produto == RecordMovimentoItem.id_produto && l.id_importacao == RecordMovimento.id_importacao).OrderBy(l => l.codigo_lote).ToList(); }
            else { ListaLotes = db.gc_estoque_lotes.Where(l => l.ativo == true && l.id_produto == RecordMovimentoItem.id_produto).OrderBy(l => l.codigo_lote).ToList(); };*/

            ListaLotes = db.gc_estoque_lotes.Where(l => l.ativo == true && l.id_produto == RecordMovimentoItem.id_produto).OrderBy(l => l.codigo_lote).ToList();

            RecordCstPedidoConferenciaEntradaLote.ComboEstoqueLotes.Add(new SelectListItem { Value = "0", Text = "Selecione um lote" });
            foreach (var lote in ListaLotes)
            {
                String TextoLote = lote.codigo_lote.EmptyIfNull().ToString().Trim();
                if (lote.codigo_serial != null) { TextoLote += " | Serial.: " + lote.codigo_serial.EmptyIfNull().ToString(); }
                if (lote.data_validade != null) { TextoLote += " | Venc.: " + lote.data_validade.Value.ToString("dd/MM/yyyy"); }
                RecordCstPedidoConferenciaEntradaLote.ComboEstoqueLotes.Add(new SelectListItem { Value = lote.id_estoque_lote.EmptyIfNull().ToString(), Text = TextoLote.EmptyIfNull().ToString() });
            }

            if (TituloAuxiliar.EmptyIfNull().ToString().Trim().Length > 0) { if (TituloAuxiliar.EmptyIfNull().ToString().Trim().Length > 120) { TituloAuxiliar = TituloAuxiliar.Substring(0, 120) + "..."; }; }
            ;
            ViewBag.TituloAuxiliar = TituloAuxiliar;
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View(RecordCstPedidoConferenciaEntradaLote);
        }

        [HttpPost]
        public ActionResult AjaxConferenciaEstoqueItem(CstPedidoConferenciaEntradaLote ViewCstPedidoConferenciaEntradaLote)
        {
            bool Sucesso = false;
            int QtdInconsistencias = 0;
            String MsgRetorno = "";
            String NumerosSeriais = string.Empty;

            try
            {
                gc_movimentos_itens RecordMovimentoItem = new gc_movimentos_itens();
                RecordMovimentoItem = db.gc_movimentos_itens.Find(ViewCstPedidoConferenciaEntradaLote.id_movimento_item);

                if (RecordMovimentoItem == null)
                {
                    QtdInconsistencias += 1;
                    MsgRetorno += " - [Item] Não localizado!<br/>";
                }
                else if (ModelState.IsValid == true)
                {
                    if ((ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena > 0) && (ViewCstPedidoConferenciaEntradaLote.item_obs.EmptyIfNull().ToString().Trim().Length == 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Informe o MOTIVO/OBS dos itens de Não Conformidade!<br/>";
                    }
                    if ((ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel + ViewCstPedidoConferenciaEntradaLote.item_qtd_falta + ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena) != (RecordMovimentoItem.quantidade))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - A Soma das QUANTIDADES (OK + Falta + Não Conforme) deverá ser igual a quantidade do Item na NF!<br/>";
                    }

                    if ((ViewCstPedidoConferenciaEntradaLote.item_peso_unit <= 0) && (ViewCstPedidoConferenciaEntradaLote.item_peso_total <= 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Informe o PESO do item [Peso Unit.] ou [Peso Total]!<br/>";
                    }
                    else if ((ViewCstPedidoConferenciaEntradaLote.item_peso_unit > 0) && (ViewCstPedidoConferenciaEntradaLote.item_peso_total > 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Não é permitido informar [Peso Unit.] e [Peso Total] simultaneamente!<br/>";
                    }
                    if ((ViewCstPedidoConferenciaEntradaLote.item_peso_unit < 0) || (ViewCstPedidoConferenciaEntradaLote.item_peso_total < 0))
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += " - Não é permitido informar peso NEGATIVO para o item!<br/>";
                    }

                    // Conferido?
                    if (QtdInconsistencias == 0)
                    {
                        var tipo = ViewCstPedidoConferenciaEntradaLote.GetType();

                        for (int i = 1; i <= 50; i++)
                        {
                            string loteNum = i.ToString("D2");
                            string propId = $"id_estoque_lote_{loteNum}";
                            string propQtd = $"lote{loteNum}_qtd";
                            string propConferido = $"lote{loteNum}_conferido";

                            var idEstoqueLote = Convert.ToInt32(tipo.GetProperty(propId)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? 0);
                            var qtd = Convert.ToDecimal(tipo.GetProperty(propQtd)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? 0);
                            var conferido = Convert.ToBoolean(tipo.GetProperty(propConferido)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? false);

                            if (idEstoqueLote > 0 && (qtd == 0 || conferido == false))
                            {
                                QtdInconsistencias += 1;
                                MsgRetorno += $"Informe [Qtd] e [Conferido] para o Lote {loteNum}<br/>";
                            }
                        }
                    }

                    // Soma total
                    if (QtdInconsistencias == 0)
                    {
                        var tipo = ViewCstPedidoConferenciaEntradaLote.GetType();
                        decimal somaLotes = 0;

                        for (int i = 1; i <= 50; i++)
                        {
                            string loteNum = i.ToString("D2");
                            string propQtd = $"lote{loteNum}_qtd";

                            somaLotes += Convert.ToDecimal(tipo.GetProperty(propQtd)?.GetValue(ViewCstPedidoConferenciaEntradaLote) ?? 0);
                        }

                        if (RecordMovimentoItem.quantidade != somaLotes)
                        {
                            QtdInconsistencias += 1;
                            MsgRetorno += "A Soma das quantidades informadas nos lotes separados deve ser igual a quantidade do item!<br/>";
                        }
                    }
                }
                else if (ModelState.IsValid == false)
                {
                    QtdInconsistencias += 1;
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                if (QtdInconsistencias == 0)
                {
                    RecordMovimentoItem.receb_import_recebido = true;
                    RecordMovimentoItem.receb_import_qtd_disponivel = ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel;
                    RecordMovimentoItem.receb_import_qtd_falta = ViewCstPedidoConferenciaEntradaLote.item_qtd_falta;
                    RecordMovimentoItem.receb_import_qtd_quarentena = ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena;
                    if (ViewCstPedidoConferenciaEntradaLote.item_peso_unit > 0)
                    {
                        RecordMovimentoItem.recebimento_peso_unit = ViewCstPedidoConferenciaEntradaLote.item_peso_unit;
                        RecordMovimentoItem.recebimento_peso_total = (ViewCstPedidoConferenciaEntradaLote.item_peso_unit * (ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel + ViewCstPedidoConferenciaEntradaLote.item_qtd_falta + ViewCstPedidoConferenciaEntradaLote.item_qtd_quarentena));
                    }
                    else if (ViewCstPedidoConferenciaEntradaLote.item_peso_total > 0)
                    {
                        RecordMovimentoItem.recebimento_peso_total = ViewCstPedidoConferenciaEntradaLote.item_peso_total;
                        RecordMovimentoItem.recebimento_peso_unit = (RecordMovimentoItem.recebimento_peso_total / ViewCstPedidoConferenciaEntradaLote.item_qtd_disponivel);
                    }
                    RecordMovimentoItem.receb_import_obs = ViewCstPedidoConferenciaEntradaLote.item_obs;


                    for (int i = 1; i <= 50; i++)
                    {
                        string nomeId = $"id_estoque_lote_{i:D2}";
                        string nomeQtd = $"lote{i:D2}_qtd";

                        var propIdOrigem = ViewCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeId);
                        var propIdDestino = RecordMovimentoItem.GetType().GetProperty(nomeId);

                        if (propIdOrigem != null && propIdDestino != null)
                            propIdDestino.SetValue(RecordMovimentoItem,
                                                   propIdOrigem.GetValue(ViewCstPedidoConferenciaEntradaLote));

                        var propQtdOrigem = ViewCstPedidoConferenciaEntradaLote.GetType().GetProperty(nomeQtd);
                        var propQtdDestino = RecordMovimentoItem.GetType().GetProperty(nomeQtd);

                        if (propQtdOrigem != null && propQtdDestino != null)
                            propQtdDestino.SetValue(RecordMovimentoItem,
                                                    propQtdOrigem.GetValue(ViewCstPedidoConferenciaEntradaLote));
                    }

                    RecordMovimentoItem.receb_import_datahora = LibDateTime.getDataHoraBrasilia();
                    RecordMovimentoItem.receb_import_id_usuario = CachePersister.userIdentity.IdUsuario;
                    RecordMovimentoItem.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    RecordMovimentoItem.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimentoItem).State = EntityState.Modified;

                    // Atualizar o peso do produto
                    g_produtos RecordProduto = db.g_produtos.Find(RecordMovimentoItem.id_produto);
                    if (RecordProduto != null)
                    {
                        RecordProduto.peso = RecordMovimentoItem.recebimento_peso_unit;
                        db.Entry(RecordProduto).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxFinalizarRecebimentoEstoque(gc_movimentos view_g_movimentos)
        {
            int QtdErros = 0;
            int QtdItensNaoConferidos = 0;
            int QtdItensNaoLocalizados = 0;

            Decimal QtdItensTotal = 0;
            Decimal QtdItensConformes = 0;
            Decimal QtdItensNaoConformes = 0;
            Decimal QtdItensQuarentena = 0;
            Decimal QtdItensFaltantes = 0;

            bool Sucesso = false;
            String MsgRetorno = "";
            String ListaIdsProdutos = string.Empty;
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(view_g_movimentos.id_movimento);
            List<gc_movimentos_itens> ListaMovimentosItensConferir = new List<gc_movimentos_itens>();
            List<gc_movimentos_itens> ListaMovimentosItensAtualizar = new List<gc_movimentos_itens>();
            List<g_produtos> ListaProdutosRelacionados = new List<g_produtos>();
            List<g_produtos> ListaProdutosAtualizar = new List<g_produtos>();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                if (RecordMovimento == null)
                {
                    QtdErros += 1;
                    MsgRetorno = "Movimento NÃO localizado no banco de dados!" + "<br/>";
                }
                else
                {
                    if (RecordMovimento.receb_estoque_processado == true)
                    {
                        QtdErros += 1;
                        MsgRetorno = "Recebimento já finalizado anteriormente!" + "<br/>";
                    }
                }


                if (QtdErros == 0)
                {
                    ListaMovimentosItensConferir = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimento.id_movimento).ToList();

                    foreach (var RecordItemConferir in ListaMovimentosItensConferir)
                    {
                        if (ListaIdsProdutos.EmptyIfNull().ToString().Trim().Length > 0) { ListaIdsProdutos += ", "; }
                        ListaIdsProdutos += RecordItemConferir.id_produto.EmptyIfNull().ToString();

                        if (RecordItemConferir.receb_import_recebido == true)
                        {
                            if (RecordItemConferir.recebimento_peso_total > 0) { RecordItemConferir.recebimento_peso_unit = (RecordItemConferir.recebimento_peso_total / RecordItemConferir.quantidade); }
                            else if (RecordItemConferir.recebimento_peso_unit > 0) { RecordItemConferir.recebimento_peso_total = (RecordItemConferir.recebimento_peso_unit * RecordItemConferir.quantidade); };

                            RecordItemConferir.receb_import_processado = true;
                            RecordItemConferir.receb_import_id_usuario = CachePersister.userIdentity.IdUsuario; ;
                            RecordItemConferir.receb_import_datahora = DataHoraAtual;
                            ListaMovimentosItensAtualizar.Add(RecordItemConferir);

                            QtdItensTotal += RecordItemConferir.receb_import_qtd_disponivel + RecordItemConferir.receb_import_qtd_quarentena + RecordItemConferir.receb_import_qtd_falta;
                            QtdItensConformes += RecordItemConferir.receb_import_qtd_disponivel;
                            QtdItensNaoConformes += RecordItemConferir.receb_import_qtd_quarentena + RecordItemConferir.receb_import_qtd_falta;
                            QtdItensQuarentena += RecordItemConferir.receb_import_qtd_quarentena;
                            QtdItensFaltantes += RecordItemConferir.receb_import_qtd_falta;
                        }
                        else
                        {
                            QtdErros += 1;
                            QtdItensNaoConferidos += 1;
                        }
                    }
                }

                if (QtdItensNaoConferidos > 0) { MsgRetorno = QtdItensNaoConferidos.ToString() + " Itens NÃO Conferidos!" + "<br/>"; };
                if (QtdItensNaoLocalizados > 0) { MsgRetorno = QtdItensNaoLocalizados.ToString() + " Itens NÃO Localizados!" + "<br/>"; };

                if (QtdErros == 0)
                {
                    // Atualizar peso dos produtos
                    ListaProdutosRelacionados = db.g_produtos.SqlQuery("select * from g_produtos where id_produto in (" + ListaIdsProdutos.EmptyIfNull().ToString() + ")").ToList();
                    foreach (var RecordItemConferir in ListaMovimentosItensConferir)
                    {
                        g_produtos RecordProdutoRelacionado = ListaProdutosRelacionados.Where(p => p.id_produto == RecordItemConferir.id_produto).FirstOrDefault();
                        if (RecordProdutoRelacionado != null)
                        {
                            if ((RecordItemConferir.recebimento_peso_unit > 0) && (RecordProdutoRelacionado.peso != RecordItemConferir.recebimento_peso_unit))
                            {
                                RecordProdutoRelacionado.peso = RecordItemConferir.recebimento_peso_unit;
                                RecordProdutoRelacionado.datahora_alteracao = DataHoraAtual;
                                RecordProdutoRelacionado.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                ListaProdutosAtualizar.Add(RecordProdutoRelacionado);
                            }
                        }
                    }
                    foreach (var RecordItemAtualizarPeso in ListaProdutosAtualizar)
                    {
                        RecordItemAtualizarPeso.datahora_alteracao = DataHoraAtual;
                        RecordItemAtualizarPeso.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordItemAtualizarPeso).State = EntityState.Modified;
                    }

                    RecordMovimento.receb_estoque_processado = true;
                    RecordMovimento.receb_estoque_id_usuario = CachePersister.userIdentity.IdUsuario;
                    RecordMovimento.receb_estoque_datahora = LibDateTime.getDataHoraBrasilia();
                    RecordMovimento.receb_estoque_qtd_itens_total = QtdItensTotal;
                    RecordMovimento.receb_estoque_qtd_itens_conformes = QtdItensConformes;
                    RecordMovimento.receb_estoque_qtd_itens_nc = QtdItensNaoConformes;
                    RecordMovimento.receb_estoque_qtd_itens_quarentena = QtdItensQuarentena;
                    RecordMovimento.receb_estoque_qtd_itens_falta = QtdItensFaltantes;
                    RecordMovimento.datahora_alteracao = DataHoraAtual;
                    RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimento).State = EntityState.Modified;
                    foreach (gc_movimentos_itens RecordMovimentosItens in ListaMovimentosItensAtualizar) 
                    {
                        RecordMovimentosItens.datahora_alteracao = DataHoraAtual;
                        RecordMovimentosItens.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordMovimentosItens).State = EntityState.Modified; 
                    };
                    db.SaveChanges();


                    int IdEstoqueMovimento = 0;
                    String NomeTipoEntrada = String.Empty;
                    if (RecordMovimento.id_movimento_tipo == 10) // Entrada - Fornecedor - Nacional
                    {
                        IdEstoqueMovimento = 7; // Entrada - Compra Nacional
                        NomeTipoEntrada = "Recebimento de NFe Entrada Nacional";
                    }
                    else if (RecordMovimento.id_movimento_tipo == 11) // Entrada - Devolução
                    {
                        IdEstoqueMovimento = 8; // Entrada - Devolução
                        NomeTipoEntrada = "Recebimento de Devolução";
                    }
                    else if (RecordMovimento.id_movimento_tipo == 18) // Entrada - Transferência entre Filiais
                    {
                        IdEstoqueMovimento = 4; // Entrada - Transferência
                        NomeTipoEntrada = "Entrada - Transferência entre filiais";
                    }
                    EstoqueInventarioService ServicoEstoqueBaixa = new EstoqueInventarioService();
                    bool EstoqueMovimentado = ServicoEstoqueBaixa.MovimentarEstoque(RecordMovimento.id_movimento, IdEstoqueMovimento, db, true); // Entrada - Compra Nacional | Entrada - Devolução | Entrada - Transferência entre Filiais
                    if (EstoqueMovimentado == false)
                    {
                        MsgRetorno += ServicoEstoqueBaixa.GetMsgProcessamento(); ;
                    }
                    else
                    {
                        Sucesso = true;
                        MsgRetorno = string.Empty;
                        MsgRetorno += NomeTipoEntrada + " concluído, e estoque atualizado com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        MsgRetorno += QtdItensTotal.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens TOTAL" + "<br/><br/>";
                        MsgRetorno += QtdItensConformes.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens VALIDADOS" + "<br/>";
                        MsgRetorno += QtdItensNaoConformes.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens NÃO CONFORMIDADE" + "<br/>";
                        MsgRetorno += QtdItensQuarentena.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens QUARENTENA" + "<br/>";
                        MsgRetorno += QtdItensFaltantes.ToString() + LibStringFormat.GetTabHtml(1) + "Quantidade itens FALTANTES" + "<br/>";
                    }
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

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        public ActionResult AjaxRelatorioPosicaoEstoque(int id)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String DirTempFiles = String.Empty;
            String IdProcessamentoGravado = "0";
            String SqlEstoque = String.Empty;
            String NumerosNotasFiscais = String.Empty;
            String NomeCliente = String.Empty;
            String DescricaoOperacao = String.Empty;
            String PedidoNumero = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_estoque_posicao.xlsx");

            List<Db.g_produtos> ListaProdutosEstoque = new List<Db.g_produtos>();
            try
            {
                SqlEstoque = "select * from g_produtos where id_produto > 0 and (saldo_01_disponivel > 0 or saldo_03_disponivel > 0) ";
                ListaProdutosEstoque = db.g_produtos.SqlQuery(SqlEstoque).ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (ListaProdutosEstoque.Count > 0)
                {
                    foreach (g_produtos RecordProduto in ListaProdutosEstoque)
                    {
                        IndexLinha += 1;
                        WorkSheet.Cell(IndexLinha, 1).Value = RecordProduto.id_produto.EmptyIfNull().ToString();
                        WorkSheet.Cell(IndexLinha, 2).Value = RecordProduto.codigo.EmptyIfNull().ToString();
                        WorkSheet.Cell(IndexLinha, 3).Value = RecordProduto.nome.EmptyIfNull().ToString();
                        WorkSheet.Cell(IndexLinha, 4).Value = RecordProduto.fob1_dollar;
                        WorkSheet.Cell(IndexLinha, 5).Value = RecordProduto.saldo_01_disponivel;
                        WorkSheet.Cell(IndexLinha, 6).Value = RecordProduto.saldo_03_disponivel;
                        NumeroRegistrosExportados += 1;
                    }

                    WorkSheet.Cell(2, 1).Value = "Posição de Estoque em " + DataHoraAtual.ToString("dd/MM/yyyy HH:mm"); ;

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_Estoque_Posicao_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);

                    //WorkSheet.Columns().AdjustToContents();
                    WorkBook.SaveAs(FileNameExportacao);
                    WorkBook.Dispose();

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                    record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                    record_g_processamento.detalhamento = "Relatório Estoque Posicao";
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
    }
}