using DocumentFormat.OpenXml.Drawing.Charts;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Robos.Itau;
using GdiPlataform.Security;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class FinanceiroLancamentosController : Controller
    {
        public string MsgGeral = string.Empty;
        public int OrdemPagamento = 0;
        private HSSFWorkbook _workbookCatalogo;
        private GdiPlataformEntities db;
        private readonly List<string> listaPdfsGerados = new List<string>();

        public FinanceiroLancamentosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-sack-dollar","", "#008000","fa-lg") + LibStringFormat.GetTabHtml(1) + "Gestão Financeira";
            PreencherLookupsIndexFinanceiro();
            LoadComboFiltroCst01();
            var viewModel = new CstModalRelatorio
            {
                Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual(),
                Field_Data_02 = LibDateTime.getUltimoDiaMesAtual()
            };
            return View(viewModel);
        }

        public void LoadComboFiltroCst01()
        {
            var comboFiltroCst01 = new List<SelectListItem>();
            comboFiltroCst01.Add(new SelectListItem { Value = "0", Text = "[ TODOS ]" });
            comboFiltroCst01.Add(new SelectListItem { Value = "1", Text = "ADIANTAMENTO" });
            comboFiltroCst01.Add(new SelectListItem { Value = "2", Text = "IMPOSTO" });
            comboFiltroCst01.Add(new SelectListItem { Value = "3", Text = "DIFAL" });
            ViewBag.comboFiltroCst01 = comboFiltroCst01;
        }
        public ActionResult GetDadosLancamentos(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage;
            string stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace;
            string filterOnOff = "0";

            try
            {
                // =========================
                // 1) Parse filtros
                // =========================
                bool filterAdvanced = false; // permanece como no seu código (sempre false)

                int idContaCaixa = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idContaCaixa);
                string filtroStatus = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                string filtroDescricao = param.yesCustomField03.EmptyIfNull().ToString().Trim();
                string filtroIdLancamento = param.yesCustomField04.EmptyIfNull().ToString().Trim();
                string filtroNumeroDocumento = param.yesCustomField05.EmptyIfNull().ToString().Trim();
                string filtroValor = param.yesCustomField06.EmptyIfNull().ToString().Trim();
                string filtroCliFor = param.yesCustomField07.EmptyIfNull().ToString().Trim();
                string filtroHideGerencial = param.yesCustomField08.EmptyIfNull().ToString().Trim();
                DateTime data1 = DateTime.Now;
                DateTime data2 = DateTime.Now;
                DateTime dataAtual = DateTime.Now.Date;
                DateTime.TryParse(param.yesCustomField09.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out data1);
                DateTime.TryParse(param.yesCustomField10.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out data2);
                string filtroCustom01 = param.yesCustomField11.EmptyIfNull().ToString().Trim();
                if (string.IsNullOrWhiteSpace(filtroCustom01)) filtroCustom01 = "0";

                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos = string.Empty;
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField01.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField02.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField03.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField04.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField05.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField06.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField07.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField08.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField09.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField10.EmptyIfNull().ToString().Trim() + ";";
                CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos += param.yesCustomField11.EmptyIfNull().ToString().Trim() + ";";

                // Em alguns filtros, seu legado expandia o período — mantive (mesmo sem aplicar datas aqui)
                if ((filtroCliFor != "-1") ||
                    (filtroDescricao.Length > 0) ||
                    (filtroIdLancamento.Length > 0) ||
                    (filtroNumeroDocumento.Length > 0) ||
                    (filtroValor.Length > 0))
                {
                    try { data1 = data1.AddYears(-10); data2 = data2.AddYears(10); } catch { }
                }

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 100 : param.iDisplayLength);

                // Indicador Limpar (Padrão B): amarelo só após Pesquisar / onchange (yesFilterField = "*")
                if (param.yesFilterField.EmptyIfNull().ToString().Trim() == "*")
                {
                    filterOnOff = "1";
                }

                // =========================
                // 2) Controle de acesso Conta Caixa (sem try/catch gigante)
                // =========================
                CachePersister.userIdentity.param_contacaixa_gc_has_edit = false;
                CachePersister.userIdentity.param_contacaixa_gc_has_view = false;
                CachePersister.userIdentity.param_contacaixa_gc_has_gerencial = false;
                CachePersister.userIdentity.IdContaCaixaAtiva = param.yesCustomField01.EmptyIfNull().ToString().Trim();

                if (CachePersister.userIdentity.IdContaCaixaAtiva == "999")
                {
                    CachePersister.userIdentity.param_contacaixa_gc_has_edit = true;
                    CachePersister.userIdentity.param_contacaixa_gc_has_view = true;
                    CachePersister.userIdentity.param_contacaixa_gc_has_gerencial = true;
                }
                else
                {
                    var acesso = db.g_contas_caixas_acessos.AsNoTracking()
                        .FirstOrDefault(a => a.id_usuario == CachePersister.userIdentity.IdUsuario && a.id_conta_caixa == idContaCaixa);

                    if (acesso != null)
                    {
                        CachePersister.userIdentity.param_contacaixa_gc_has_edit = acesso.has_edit;
                        CachePersister.userIdentity.param_contacaixa_gc_has_view = acesso.has_view;
                        CachePersister.userIdentity.param_contacaixa_gc_has_gerencial = acesso.has_gerencial;
                    }
                }

                // =========================
                // 3) Query base EF (sem SQL concatenado)
                // =========================
                var lanc = db.gc_financeiro_lancamentos.AsNoTracking().Where(l => l.id_usuario_cadastro > 0 && l.ativo == true);
                if (idContaCaixa < 999) lanc = lanc.Where(l => l.id_conta_caixa == idContaCaixa);
                lanc = lanc.Where(l => l.data_pagamento >= data1 && l.data_pagamento <= data2);

                // Observação importante:
                // Seu legado tinha filtros de STATUS comentados por /* ... */ e só "descomentava" quando FilterAdvanced==false,
                // porém o bloco inteiro ficava dentro do comentário. Na prática, STATUS não filtrava.
                // Para manter o comportamento, não aplico filtroStatus aqui.

                // Status
                if (!string.IsNullOrWhiteSpace(filtroStatus))
                {
                    if (filtroStatus != "0") { if (int.TryParse(filtroStatus, out int idStatus)) lanc = lanc.Where(l => l.id_financeiro_status == idStatus); };
                }

                // Gerencial
                if (!CachePersister.userIdentity.param_contacaixa_gc_has_gerencial)
                    lanc = lanc.Where(l => l.gerencial == false);
                else if (!string.IsNullOrWhiteSpace(filtroHideGerencial))
                    lanc = lanc.Where(l => l.gerencial == false);

                // Filtros texto
                if (LibStringFormat.TryMontarPadraoLikeContemTexto(filtroDescricao, out string padraoDescricao))
                {
                    lanc = lanc.Where(l => l.descricao != null && DbFunctions.Like(l.descricao, padraoDescricao));
                }

                if (LibStringFormat.TryMontarPadraoLikeContemCodigo(filtroNumeroDocumento, out string padraoNumeroDoc))
                {
                    lanc = lanc.Where(l => l.numero_documento != null && DbFunctions.Like(l.numero_documento, padraoNumeroDoc));
                }

                if (!string.IsNullOrWhiteSpace(filtroIdLancamento))
                {
                    if (int.TryParse(filtroIdLancamento, out int idLan))
                        lanc = lanc.Where(l => l.id_lancamento == idLan);
                    else
                        lanc = lanc.Where(l => false);
                }

                if (!string.IsNullOrWhiteSpace(filtroCliFor) && filtroCliFor != "0" && filtroCliFor != "-1")
                {
                    if (int.TryParse(filtroCliFor, out int idCli))
                        lanc = lanc.Where(l => l.id_cliente == idCli);
                    else
                        lanc = lanc.Where(l => false);
                }

                if (!string.IsNullOrWhiteSpace(filtroValor))
                {
                    if (decimal.TryParse(filtroValor, NumberStyles.Any, new CultureInfo("pt-BR"), out decimal v) ||
                        decimal.TryParse(filtroValor, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    {
                        lanc = lanc.Where(l => l.valor_total == v || l.valor_pago == v);
                    }
                }
                
                // Custom categoria
                if (filtroCustom01 == "1") lanc = lanc.Where(l => l.is_adiantamento == true);
                else if (filtroCustom01 == "2") lanc = lanc.Where(l => l.is_provisao_imposto == true);
                else if (filtroCustom01 == "3") lanc = lanc.Where(l => l.is_difal == true);

                // =========================
                // 4) Ordenação (OrderBy antes do Skip/Take)
                // =========================
                // Igual sua intenção: conta + COALESCE(data_pagamento, data_vencimento) + ordem_pagamento
                var ordered = lanc
                    .OrderBy(l => l.id_conta_caixa)
                    .ThenByDescending(l => (DateTime?)l.data_pagamento ?? l.data_vencimento)
                    .ThenByDescending(l => l.ordem_pagamento)
                    .ThenByDescending(l => l.id_lancamento);

                // Totais DataTables
                int totalRecords = ordered.Count();
                int totalDisplayRecords = totalRecords;

                // =========================
                // 5) Página (somente campos necessários)
                // =========================
                var page = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(l => new
                    {
                        // keys / relacionamento
                        l.id_lancamento,
                        l.id_conta_caixa,
                        l.id_cliente,
                        l.id_tablerow_color,

                        // flags e status
                        l.tipo_pag_rec,
                        l.gerencial,
                        l.id_financeiro_status,
                        l.fixo,
                        l.parcela_atual,
                        l.parcela_total,

                        // datas
                        l.data_pagamento,
                        l.data_vencimento,

                        // textos / docs
                        l.numero_documento,
                        l.descricao,
                        l.cnab_linha_digitavel,

                        // valores
                        l.valor_total,
                        l.valor_pago,

                        // categorias
                        l.is_adiantamento,
                        l.is_provisao_imposto,
                        l.is_difal,
                        l.is_boleto,
                        l.id_lancamento_adiantamento,

                        // negociação
                        l.negociacao,
                        l.negociacao_data_limite
                    })
                    .ToList();

                // =========================
                // 6) Pareamento especial (id_tablerow_color==3 e numero_documento) - sem carregar "allRecords"
                // =========================
                // Regras do legado (para os itens da página): procura "par" com mesma cor e mesmo numero_documento
                var docsParear = page
                    .Where(x => x.id_tablerow_color == 3 && !string.IsNullOrWhiteSpace(x.numero_documento))
                    .Select(x => x.numero_documento)
                    .Distinct()
                    .ToList();

                List<dynamic> pares = new List<dynamic>();
                if (docsParear.Count > 0)
                {
                    var idsPagina = page.Select(x => x.id_lancamento).ToList();

                    pares = db.gc_financeiro_lancamentos.AsNoTracking()
                        .Where(l => l.ativo == true
                                    && l.id_usuario_cadastro > 0
                                    && l.id_tablerow_color == 3
                                    && docsParear.Contains(l.numero_documento)
                                    && !idsPagina.Contains(l.id_lancamento))
                        .Select(l => new
                        {
                            l.id_lancamento,
                            l.id_conta_caixa,
                            l.id_cliente,
                            l.id_tablerow_color,

                            l.tipo_pag_rec,
                            l.gerencial,
                            l.id_financeiro_status,
                            l.fixo,
                            l.parcela_atual,
                            l.parcela_total,

                            l.data_pagamento,
                            l.data_vencimento,

                            l.numero_documento,
                            l.descricao,
                            l.cnab_linha_digitavel,

                            l.valor_total,
                            l.valor_pago,

                            l.is_adiantamento,
                            l.is_provisao_imposto,
                            l.is_difal,
                            l.is_boleto,
                            l.id_lancamento_adiantamento,

                            l.negociacao,
                            l.negociacao_data_limite
                        })
                        .ToList<dynamic>();
                }

                var finalRows = new List<dynamic>(page.Count + 10);

                foreach (var g in page)
                {
                    if (finalRows.Any(r => (int)r.id_lancamento == g.id_lancamento)) continue;

                    if (g.id_tablerow_color == 3 && !string.IsNullOrWhiteSpace(g.numero_documento))
                    {
                        var p = pares.FirstOrDefault(x => (string)x.numero_documento == g.numero_documento);
                        if (p != null)
                        {
                            // regra original: se valor_total do general > validate, adiciona os dois
                            if (g.valor_total > (decimal)p.valor_total)
                            {
                                finalRows.Add(g);
                                finalRows.Add(p);
                                continue;
                            }
                        }
                    }

                    finalRows.Add(g);
                }

                // =========================
                // 7) Lookups (somente IDs presentes em finalRows)
                // =========================
                var idsCC = finalRows.Select(x => (int)x.id_conta_caixa).Where(x => x > 0).Distinct().ToList();
                var idsCli = finalRows.Select(x => (int)x.id_cliente).Where(x => x > 0).Distinct().ToList();
                var idsColor = finalRows.Select(x => (int)x.id_tablerow_color).Where(x => x > 0).Distinct().ToList();

                var contasDict = db.g_contas_caixas.AsNoTracking()
                    .Where(c => idsCC.Contains(c.id_conta_caixa))
                    .Select(c => new { c.id_conta_caixa, c.nome })
                    .ToList()
                    .ToDictionary(x => x.id_conta_caixa, x => x.nome);

                var clientesDict = db.g_clientes.AsNoTracking()
                    .Where(c => idsCli.Contains(c.id_cliente))
                    .Select(c => new { c.id_cliente, c.nome })
                    .ToList()
                    .ToDictionary(x => x.id_cliente, x => x.nome);

                var colorsDict = db.a_tablesrows_colors.AsNoTracking()
                    .Where(c => idsColor.Contains(c.id_tablerow_color))
                    .Select(c => new { c.id_tablerow_color, c.collor })
                    .ToList()
                    .ToDictionary(x => x.id_tablerow_color, x => x.collor);

                // =========================
                // 8) Saldo Conta Caixa (mantive seu SQL agregado)
                // =========================
                string saldoContaCaixa;
                try
                {
                    string sqlSaldo = @"
                                    select l.id_conta_caixa, c.tag_saldo_dia,
                                        SUM(CASE WHEN tipo_pag_rec = 1 THEN valor_pago ELSE 0 END) total_pago,
                                        SUM(CASE WHEN tipo_pag_rec = 2 THEN valor_pago ELSE 0 END) total_recebido
                                    from gc_financeiro_lancamentos l
                                    left join g_contas_caixas c on (l.id_conta_caixa = c.id_conta_caixa)
                                    where (c.id_conta_caixa > 0) and (c.ativo = 1) and (l.ativo = 1)
                                      and (l.id_financeiro_status != 3) and (l.id_financeiro_status != 5)
                                      and (l.data_pagamento >= '2022-06-01 00:00:00')
                                    ";

                    if (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "999") sqlSaldo += $" and (l.id_conta_caixa = {idContaCaixa}) ";
                    sqlSaldo += " group by l.id_conta_caixa, c.tag_saldo_dia";
                    var table = LibDB.GetDataTable(sqlSaldo, db);

                    if (table.Rows.Count == 0) 
                    { 
                        saldoContaCaixa = "0,00"; 
                    }
                    else
                    {
                        decimal saldoGeral = 0;

                        foreach (DataRow row in table.Rows)
                        {
                            decimal totalPago = decimal.Parse(row["total_pago"].EmptyIfNull().ToString().Trim());
                            decimal totalReceb = decimal.Parse(row["total_recebido"].EmptyIfNull().ToString().Trim());
                            decimal saldoCC = totalReceb - totalPago;

                            if (param.yesCustomField01.EmptyIfNull().ToString().Trim() == "999")
                            {
                                string tag = row["tag_saldo_dia"].EmptyIfNull().ToString().Trim();
                                if (tag == "+") saldoGeral += saldoCC;
                                if (tag == "-") saldoGeral -= saldoCC;
                            }
                            else
                            {
                                saldoGeral += saldoCC;
                            }
                        }

                        saldoContaCaixa = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", saldoGeral)
                            .Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                    }
                }
                catch
                {
                    saldoContaCaixa = "Erro";
                }

                CachePersister.userIdentity.SaldoContaCaixaAtiva = saldoContaCaixa;

                // =========================
                // 9) Monta aaData
                // =========================
                var list = new List<string[]>(finalRows.Count);

                foreach (var l in finalRows)
                {
                    int idLanc = (int)l.id_lancamento;
                    int idCC = (int)l.id_conta_caixa;
                    int idCliRow = (int)l.id_cliente;

                    string nomeConta = (idCC > 0 && contasDict.TryGetValue(idCC, out var ccNome)) ? ccNome : "NÃO IDENTIFICADA";

                    string nomeCliente = (idCliRow > 0 && clientesDict.TryGetValue(idCliRow, out var cNome)) ? cNome : "";
                    string desc = ((string)l.descricao ?? "").Trim();

                    if (desc.Length > 0)
                    {
                        if (nomeCliente.Length > 0) nomeCliente += " - ";
                        nomeCliente += desc;
                    }

                    bool fixo = (bool)l.fixo;
                    if (!fixo)
                        nomeCliente += " (" + l.parcela_atual.ToString() + "/" + l.parcela_total.ToString() + ")";

                    // negociação
                    bool negociacao = (bool)l.negociacao;
                    int tipoPagRec = (int)l.tipo_pag_rec;
                    bool isProvisao = (bool)l.is_provisao_imposto;
                    bool isDifal = (bool)l.is_difal;
                    int idStatusFin = (int)l.id_financeiro_status;

                    if (negociacao && tipoPagRec == 2 && !isProvisao && !isDifal && idStatusFin == 3)
                    {
                        DateTime? limite = (DateTime?)l.negociacao_data_limite;
                        if (limite.HasValue)
                        {
                            if (dataAtual <= limite.Value.Date)
                                nomeCliente += "<br/>" + LibIcons.getIcon("fa-solid fa-handshake", "", "orange", "fa-sm") +
                                              "   <font color=\"#ce7e00\">[Em Negociação até " + limite.Value.ToString("dd/MM/yyyy") + "]</font>";
                            else
                                nomeCliente += "<br/>" + LibIcons.getIcon("fa-solid fa-handshake-slash", "", "red", "fa-sm") +
                                              "   <font color=\"#cc0000\">[Negociação vencida em " + limite.Value.ToString("dd/MM/yyyy") + "]</font>";
                        }
                    }

                    // cor da linha
                    string rowColor = "";
                    int idColor = (int)l.id_tablerow_color;
                    if (idColor > 0 && colorsDict.TryGetValue(idColor, out var corRow)) rowColor = corRow;

                    // liquidado com adiantamento
                    int idAd = (int)l.id_lancamento_adiantamento;
                    if (idStatusFin == 1 && tipoPagRec == 2 && idAd > 0)
                        nomeCliente += "<br/>[ Liquidado com adiantamento ]";

                    // ícones status
                    string iconeStatus =
                        idStatusFin == 1 ? LibIcons.getIcon("fa-solid fa-folder-closed", "Liquidado", "green", "fa-sm") :
                        idStatusFin == 2 ? LibIcons.getIcon("fa-solid fa-folder-open", "Baixa Parcial", "orange", "fa-sm") :
                        idStatusFin == 3 ? LibIcons.getIcon("fa-solid fa-folder-open", "Aberto", "gray", "fa-sm") :
                                           LibIcons.getIcon("fa-solid fa-folder-minus", "Cancelado", "red", "fa-sm");

                    bool hasBoleto = (((string)l.cnab_linha_digitavel ?? "").Length > 0);
                    bool gerencial = (bool)l.gerencial;

                    string iconeTipoPagRec = "";
                    if (gerencial)
                    {
                        if (tipoPagRec == 1) iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-money-bill-trend-up", "Débito Gerencial", "gray", "fa-sm");
                        else iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-cash-register", hasBoleto ? "Crédito Gerencial (Boleto)" : "Crédito Gerencial", "gray", "fa-sm");
                    }
                    else
                    {
                        if (tipoPagRec == 1) iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-money-bill-trend-up", "Débito", "gray", "fa-sm");
                        else iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-cash-register", hasBoleto ? "Crédito (Boleto)" : "Crédito", "gray", "fa-sm");
                    }

                    // ícone categoria
                    string iconeCategoria = "";
                    if ((bool)l.is_adiantamento) iconeCategoria = LibIcons.getIcon("fa-solid fa-wallet", "Adiantamento", "gray", "fa-sm");
                    else if ((bool)l.is_provisao_imposto) iconeCategoria = LibIcons.getIcon("fa-solid fa-i", "Provisão Imposto", "gray", "fa-sm");
                    else if ((bool)l.is_difal) iconeCategoria = LibIcons.getIcon("fa-solid fa-d", "Difal", "gray", "fa-sm");
                    else if ((bool)l.is_boleto) iconeCategoria = LibIcons.getIcon("fa-solid fa-barcode", "Boleto", "gray", "fa-sm");
                    else if (tipoPagRec == 2) iconeCategoria = LibIcons.getIcon("fa-solid fa-money-bill-1", "Ted/Doc/Pix", "gray", "fa-sm");
                    else iconeCategoria = LibIcons.getIcon("fa-solid fa-credit-card", "Cartão Crédito", "gray", "fa-sm");

                    // valores
                    decimal valorTotal = (decimal)l.valor_total;
                    decimal valorPago = (decimal)l.valor_pago;

                    string valorTotalStr = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                    string valorPagoStr = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorPago).Replace("R$ ", "").Replace("R$", "").Replace("$", "");

                    if (tipoPagRec == 1)
                    {
                        if (valorTotal > 0) valorTotalStr = "-" + valorTotalStr;
                        if (valorPago > 0) valorPagoStr = "-" + valorPagoStr;
                    }

                    // datas
                    DateTime? dtPag = (DateTime?)l.data_pagamento;
                    string dataPagamento = dtPag.HasValue ? dtPag.Value.ToString("dd/MM/yy") : "";
                    dataPagamento = dataPagamento.Replace("01/01/01", "").Replace("01/01/99", "");

                    DateTime? dtVenc = (DateTime?)l.data_vencimento;
                    string dataVenc = dtVenc.HasValue ? dtVenc.Value.ToString("dd/MM/yy") : "";

                    list.Add(new[]
                    {
                        rowColor,
                        "", // seleção
                        idLanc.ToString(),
                        nomeConta,
                        iconeTipoPagRec,
                        iconeStatus,
                        iconeCategoria,
                        dataPagamento,
                        dataVenc,
                        ((string)l.numero_documento ?? ""),
                        nomeCliente,
                        valorTotalStr,
                        valorPagoStr,
                        "", // anexo
                        ""  // editar
                    });
                }

                string uiFilterOnOff = filterAdvanced ? "1" : "0";

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesDisplayField01 = saldoContaCaixa,
                    yesFilterOnOff = uiFilterOnOff,
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
        public ActionResult GetDadosLancamentosByMovimento(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage;
            string stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace;
            string filterOnOff = "0";

            try
            {
                // -------------------------
                // 1) Movimento ativo
                // -------------------------
                int idMovimento = 0;
                int.TryParse(CachePersister.userIdentity.IdGcMovimentoAtivo.EmptyIfNull().ToString().Trim(), out idMovimento);

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 50 : param.iDisplayLength);

                // -------------------------
                // 2) Base query (sem SQL string)
                // -------------------------
                var query = db.gc_financeiro_lancamentos
                    .AsNoTracking()
                    .Where(l => l.ativo == true && l.id_movimento == idMovimento);

                // ordenação (igual sua intenção)
                var ordered = query
                    .OrderByDescending(l => (DateTime?)l.data_pagamento ?? l.data_vencimento)
                    .ThenByDescending(l => l.ordem_pagamento)
                    .ThenByDescending(l => l.id_lancamento);

                int totalRecords = ordered.Count();
                int totalDisplayRecords = totalRecords;

                // -------------------------
                // 3) Página (somente campos necessários)
                // -------------------------
                var page = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(l => new
                    {
                        l.id_lancamento,
                        l.tipo_pag_rec,
                        l.id_financeiro_status,
                        l.id_pag_rec_tipo,
                        l.data_pagamento,
                        l.data_vencimento,
                        l.numero_documento,
                        l.descricao,
                        l.valor_total,
                        l.valor_pago,
                        l.cnab_linha_digitavel
                    })
                    .ToList();

                // -------------------------
                // 4) Lookup PagRecTipos (só o que aparece na página)
                // -------------------------
                var idsPagRecTipos = page
                    .Select(x => (int?)x.id_pag_rec_tipo)
                    .Where(x => x.HasValue && x.Value > 0)
                    .Select(x => x.Value)
                    .Distinct()
                    .ToList();

                var pagRecTiposDict = db.g_pagrec_tipos.AsNoTracking()
                    .Where(t => idsPagRecTipos.Contains(t.id_pagrec_tipo))
                    .Select(t => new { t.id_pagrec_tipo, t.descricao })
                    .ToList()
                    .ToDictionary(x => x.id_pagrec_tipo, x => x.descricao);

                // -------------------------
                // 5) Monta aaData
                // -------------------------
                var list = new List<string[]>(page.Count);

                foreach (var l in page)
                {
                    string iconeStatus =
                        l.id_financeiro_status == 1 ? LibIcons.getIcon("fa-solid fa-sack-dollar", "Liquidado", "green", "fa-sm") :
                        l.id_financeiro_status == 2 ? LibIcons.getIcon("fa-solid fa-sack-dollar", "Baixa Parcial", "orange", "fa-sm") :
                        l.id_financeiro_status == 3 ? LibIcons.getIcon("fa-solid fa-sack-dollar", "Aberto", "gray", "fa-sm") :
                                                      LibIcons.getIcon("fa-solid fa-xmark", "Cancelado", "red", "fa-sm");

                    string descPagRecTipo = "";
                    if (l.id_pag_rec_tipo > 0 && pagRecTiposDict.TryGetValue(l.id_pag_rec_tipo, out var d))
                        descPagRecTipo = d.EmptyIfNull().ToString();

                    string iconeTipoPagRec = "";
                    if (l.tipo_pag_rec == 1)
                        iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-square-minus", $"Débito ({descPagRecTipo})", "red", "fa-sm");
                    else if (l.tipo_pag_rec == 2)
                        iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-square-plus", $"Crédito ({descPagRecTipo})", "green", "fa-sm");

                    string valorTotal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.valor_total)
                        .Replace("R$ ", "").Replace("R$", "").Replace("$", "");

                    string valorPago = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.valor_pago)
                        .Replace("R$ ", "").Replace("R$", "").Replace("$", "");

                    if (l.tipo_pag_rec == 1)
                    {
                        if (l.valor_total > 0) valorTotal = "-" + valorTotal;
                        if (l.valor_pago > 0) valorPago = "-" + valorPago;
                    }

                    string dataPag = l.data_pagamento.ToString("dd/MM/yy") ?? "";
                    string dataVenc = l.data_vencimento.ToString("dd/MM/yy") ?? "";

                    list.Add(new[]
                    {
                l.id_lancamento.ToString(),
                iconeTipoPagRec,
                iconeStatus,
                dataPag,
                dataVenc,
                l.numero_documento.EmptyIfNull().ToString(),
                l.descricao.EmptyIfNull().ToString(),
                valorTotal,
                valorPago,
                "", // Botão PDF
                l.cnab_linha_digitavel.EmptyIfNull().ToString().Trim()
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
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
                        catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }
        public ActionResult ModalCreateEditLancamento(int? IdLancamento, int? TipoPagRec, int? IdLancamentoDuplicate)
        {
            bool BloquearLancamentoProvisaoImposto = false;
            gc_financeiro_lancamentos record_gc_financeiro_lancamentos = null;
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia().Date;

            try
            {
                if (IdLancamento >= 0)
                {
                    if (IdLancamento > 0)
                    {
                        record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(IdLancamento);
                        if (record_gc_financeiro_lancamentos == null)
                        {
                            ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Lançamento financeiro", IdLancamento);
                            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Lançamento — (não localizado)</b>";
                            PreencherLookupsModalLancamento(0);
                            return View("ModalCreateEditLancamento", new gc_financeiro_lancamentos { id_lancamento = IdLancamento.GetValueOrDefault() });
                        }
                    }
                    else if (IdLancamento == 0)
                    {
                        int idLancamentoDuplicate = IdLancamentoDuplicate.GetValueOrDefault();
                        if (idLancamentoDuplicate <= 0)
                        {
                            ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Lançamento financeiro", null);
                            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Duplicar Lançamento — (não localizado)</b>";
                            PreencherLookupsModalLancamento(0);
                            return View("ModalCreateEditLancamento", new gc_financeiro_lancamentos());
                        }
                        gc_financeiro_lancamentos OldLancamento = db.gc_financeiro_lancamentos.Find(idLancamentoDuplicate);
                        if (OldLancamento == null)
                        {
                            ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Lançamento financeiro", idLancamentoDuplicate);
                            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Duplicar Lançamento — (não localizado)</b>";
                            PreencherLookupsModalLancamento(0);
                            return View("ModalCreateEditLancamento", new gc_financeiro_lancamentos { id_lancamento = 0 });
                        }
                        record_gc_financeiro_lancamentos = LibDB.CloneTObject(OldLancamento);
                        record_gc_financeiro_lancamentos.id_lancamento = 0;
                        record_gc_financeiro_lancamentos.id_lancamento_adiantamento = 0;
                        record_gc_financeiro_lancamentos.id_lancamento_adiantamento2 = 0;
                        record_gc_financeiro_lancamentos.id_lancamento_adiantamento3 = 0;
                    }

                    if (record_gc_financeiro_lancamentos.is_provisao_imposto == true) { BloquearLancamentoProvisaoImposto = true; } // Não é Permitido lançar provisão de impostos de lançamentos de provisão | O Botão não irá aparecer
                    if (record_gc_financeiro_lancamentos.id_lancamento_tributos > 0) { BloquearLancamentoProvisaoImposto = true; } // Não é Permitido lançar provisão de impostos de lançamentos de provisão | O Botão não irá aparecer
                    if ((record_gc_financeiro_lancamentos.id_conta_caixa != 2) && (record_gc_financeiro_lancamentos.id_conta_caixa != 11)) { BloquearLancamentoProvisaoImposto = true; } // Não é Permitido lançar provisão de impostos de lançamentos de provisão | O Botão não irá aparecer
                    if (record_gc_financeiro_lancamentos.tipo_pag_rec != 2) { BloquearLancamentoProvisaoImposto = true; } // Não é Permitido lançar provisão de impostos de lançamentos de provisão | O Botão não irá aparecer
                    if (record_gc_financeiro_lancamentos.id_movimento == 0) { BloquearLancamentoProvisaoImposto = true; } // Não é Permitido lançar provisão de impostos de lançamentos de provisão | O Botão não irá aparecer
                    if (record_gc_financeiro_lancamentos.negociacao == false) { record_gc_financeiro_lancamentos.negociacao_data_limite = DataAtual.AddDays(7); }
                    if (BloquearLancamentoProvisaoImposto == true)
                    {
                        record_gc_financeiro_lancamentos.id_lancamento_tributos = -1;
                    }
                    else if (BloquearLancamentoProvisaoImposto == false)
                    {
                        if (record_gc_financeiro_lancamentos.id_movimento > 0)
                        {
                            gc_movimentos record_gc_movimentos = db.gc_movimentos.Find(record_gc_financeiro_lancamentos.id_movimento);
                            if (record_gc_movimentos != null)
                            {
                                if (record_gc_movimentos.param_reducao_bc == true)
                                {
                                    record_gc_financeiro_lancamentos.tag_provisao_imposto_normal = false;
                                    record_gc_financeiro_lancamentos.tag_provisao_imposto_reduzido = true;
                                }
                                else if (record_gc_movimentos.param_reducao_bc == false)
                                {
                                    record_gc_financeiro_lancamentos.tag_provisao_imposto_normal = true;
                                    record_gc_financeiro_lancamentos.tag_provisao_imposto_reduzido = false;
                                }
                            }
                        }
                    }
                    var comboAdiantamentos = new List<SelectListItem>();
                    try
                    {
                        String SentencaSQL = string.Empty;
                        List<gc_financeiro_lancamentos> listaDbAdiantamentos = new List<gc_financeiro_lancamentos>();
                        comboAdiantamentos.Add(new SelectListItem { Value = "0", Text = "[ SEM ADIANTAMENTO ]" });
                        if (record_gc_financeiro_lancamentos.id_lancamento_adiantamento == 0)
                        {
                            SentencaSQL += "select l.* from gc_financeiro_lancamentos l ";
                            SentencaSQL += "     where (l.ativo = 1) and (l.is_adiantamento = 1) and (l.valor_pago > 0) and (l.id_financeiro_status = 1) and (l.tipo_pag_rec = 2) ";
                            SentencaSQL += "     and (l.id_cliente = " + record_gc_financeiro_lancamentos.id_cliente.EmptyIfNull().ToString() + ")";
                            SentencaSQL += "     and (l.id_lancamento != " + record_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToString() + ")";
                            SentencaSQL += "     and (l.id_lancamento not in (select distinct id_lancamento_adiantamento from gc_financeiro_lancamentos UNION ALL select distinct id_lancamento_adiantamento2 from gc_financeiro_lancamentos UNION ALL select distinct id_lancamento_adiantamento3 from gc_financeiro_lancamentos))";
                            listaDbAdiantamentos = db.gc_financeiro_lancamentos.SqlQuery(SentencaSQL).ToList();
                        }
                        else
                        {
                            SentencaSQL += "select l.* from gc_financeiro_lancamentos l where ";
                            SentencaSQL += "  ( ";
                            SentencaSQL += "       (l.id_lancamento = " + record_gc_financeiro_lancamentos.id_lancamento_adiantamento.EmptyIfNull().ToString() + ") ";
                            SentencaSQL += "    or (l.id_lancamento = " + record_gc_financeiro_lancamentos.id_lancamento_adiantamento2.EmptyIfNull().ToString() + ") ";
                            SentencaSQL += "    or (l.id_lancamento = " + record_gc_financeiro_lancamentos.id_lancamento_adiantamento3.EmptyIfNull().ToString() + ") ";
                            SentencaSQL += "  ) ";
                            listaDbAdiantamentos = db.gc_financeiro_lancamentos.SqlQuery(SentencaSQL).ToList();
                        }
                        foreach (var item_gc_financeiro_lancamentos in listaDbAdiantamentos)
                        {
                            String itemAdiantamento = "Id: " + item_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToString() + " | ";
                            itemAdiantamento += "Data: " + item_gc_financeiro_lancamentos.data_pagamento.ToString("dd/MM/yyyy") + " | ";
                            itemAdiantamento += "Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item_gc_financeiro_lancamentos.valor_pago).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                            comboAdiantamentos.Add(new SelectListItem { Value = item_gc_financeiro_lancamentos.id_lancamento.ToString(), Text = itemAdiantamento });
                        }
                    }
                    finally { }

                    // Data de liquidação do boleto, considerar o dia útil anterior

                    if ((record_gc_financeiro_lancamentos.tipo_pag_rec == 2) && (record_gc_financeiro_lancamentos.is_boleto == true) && (record_gc_financeiro_lancamentos.id_financeiro_status == 3))
                    {
                        
                        record_gc_financeiro_lancamentos.data_pagamento = LibDateTime.GetDiaUtilAnterior(DataAtual);
                        record_gc_financeiro_lancamentos.datahora_liquidacao = LibDateTime.GetDiaUtilAnterior(DataAtual);
                    }
                    ViewBag.comboAdiantamentos = comboAdiantamentos;
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Lançamento - " + record_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToString() + "</b>";
                }
                else
                {
                    record_gc_financeiro_lancamentos = new Db.gc_financeiro_lancamentos();
                    record_gc_financeiro_lancamentos.id_lancamento_origem = 0;
                    record_gc_financeiro_lancamentos.ativo = true;
                    record_gc_financeiro_lancamentos.gerencial = false;
                    record_gc_financeiro_lancamentos.fixo = false;
                    record_gc_financeiro_lancamentos.parcela_atual = 1;
                    record_gc_financeiro_lancamentos.parcela_total = 1;
                    record_gc_financeiro_lancamentos.id_financeiro_status = 1;
                    record_gc_financeiro_lancamentos.data_pagamento = DataAtual;
                    record_gc_financeiro_lancamentos.data_vencimento = DataAtual;
                    record_gc_financeiro_lancamentos.data_vencimento_original = DataAtual;
                    record_gc_financeiro_lancamentos.ordem_pagamento = 0;
                    record_gc_financeiro_lancamentos.id_lancamento_tributos = 0; // É Permitido lançar provisão de impostos
                    if (IdLancamento < 0) { record_gc_financeiro_lancamentos.id_conta_caixa = IdLancamento.GetValueOrDefault() * -1; };

                    if (TipoPagRec == 1)
                    {
                        record_gc_financeiro_lancamentos.tipo_pag_rec = 1;
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-money-bill", "Novo Pagamento", "#cc0000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Novo Pagamento</b>";
                    }
                    else if (TipoPagRec == 2)
                    {
                        record_gc_financeiro_lancamentos.tipo_pag_rec = 2;
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Novo Recebimento</b>";
                    }
                    else
                    {
                        record_gc_financeiro_lancamentos.tipo_pag_rec = 1;
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Novo Lançamento</b>";
                    }
                }
                PreencherLookupsModalLancamento(record_gc_financeiro_lancamentos.id_cliente);
                return View("ModalCreateEditLancamento", record_gc_financeiro_lancamentos);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroLancamentoController";
                msg += "<br/>" + "ModalCreateEditLancamento";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxLiquidarLancamento(gc_financeiro_lancamentos view_gc_financeiro_lancamentos)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            view_gc_financeiro_lancamentos.id_financeiro_status = 1;
            view_gc_financeiro_lancamentos.datahora_liquidacao = view_gc_financeiro_lancamentos.data_pagamento;
            if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento > 0) { view_gc_financeiro_lancamentos.valor_pago = 0; }
            else if (view_gc_financeiro_lancamentos.valor_pago <= 0) { view_gc_financeiro_lancamentos.valor_pago = view_gc_financeiro_lancamentos.valor_total; }
            return AjaxCreateEditLancamento(view_gc_financeiro_lancamentos);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxCreateEditLancamento(gc_financeiro_lancamentos view_gc_financeiro_lancamentos)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            String msgRetorno = "";
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia().Date;
            int IdMovimentoRelacionado = view_gc_financeiro_lancamentos.id_movimento;
            gc_financeiro_lancamentos record_old_gc_financeiro_lancamentos = new Db.gc_financeiro_lancamentos();
            if (IdMovimentoRelacionado > 0) { record_old_gc_financeiro_lancamentos = LibDB.CloneTObject(db.gc_financeiro_lancamentos.Find(view_gc_financeiro_lancamentos.id_lancamento)); };

            try
            {
                if (ModelState.IsValid)
                {
                    view_gc_financeiro_lancamentos.descricao = LibStringFormat.FormatarTextoCadastroNormal(view_gc_financeiro_lancamentos.descricao);
                    if (view_gc_financeiro_lancamentos.numero_documento != null) 
                    { 
                        view_gc_financeiro_lancamentos.numero_documento = LibStringFormat.FormatarTextoCadastroNormal(view_gc_financeiro_lancamentos.numero_documento);
                        if ((view_gc_financeiro_lancamentos.id_movimento == 0) || (view_gc_financeiro_lancamentos.id_movimento_nf == 0))
                        {
                            gc_movimentos_nf RecordNotasFiscais = db.gc_movimentos_nf.Where(nf => nf.nf_numero == view_gc_financeiro_lancamentos.numero_documento).FirstOrDefault();
                            if (RecordNotasFiscais != null)
                            {
                                view_gc_financeiro_lancamentos.id_movimento = RecordNotasFiscais.id_movimento;
                                view_gc_financeiro_lancamentos.id_movimento_nf = RecordNotasFiscais.id_movimento_nf;
                            }
                        }
                    };

                    if (view_gc_financeiro_lancamentos.descricao.EmptyIfNull().ToString() != String.Empty) { view_gc_financeiro_lancamentos.descricao = LibStringFormat.FormatarTextoCadastroNormal(view_gc_financeiro_lancamentos.descricao); }

                    if ((view_gc_financeiro_lancamentos.valor_total <= 0) && (view_gc_financeiro_lancamentos.valor_pago > 0))
                    {
                        view_gc_financeiro_lancamentos.valor_total = view_gc_financeiro_lancamentos.valor_pago;
                    }
                    if (view_gc_financeiro_lancamentos.id_cliente <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [Cliente/Fornecedor] é de preenchimento obrigatório!<br/>";
                    }
                    else
                    {
                        g_clientes record_g_clientes = db.g_clientes.Find(view_gc_financeiro_lancamentos.id_cliente);
                        if ((view_gc_financeiro_lancamentos.tipo_pag_rec == 2) && (record_g_clientes.is_cliente == false))
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - [Cliente/Fornecedor] informado NÃO está cadastrado como Cliente!<br/>";
                        }
                        if ((view_gc_financeiro_lancamentos.id_cliente == 704) && (view_gc_financeiro_lancamentos.descricao.EmptyIfNull().ToString().Length == 0)) // GDI
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - [Descrição] é obrigatório quando não se informa Cliente/Fornecedor!<br/>";
                        }
                    }
                    if (view_gc_financeiro_lancamentos.id_classificacao_financeira <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [Classificação Financeira] é de preenchimento obrigatório!<br/>";
                    }
                    else
                    {
                        g_classificacao_financeira RecordClassificacaoFinanceira = db.g_classificacao_financeira.Find(view_gc_financeiro_lancamentos.id_classificacao_financeira);
                        if ((view_gc_financeiro_lancamentos.tipo_pag_rec == 1) && (RecordClassificacaoFinanceira.debito == false))
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Classificação Financeira deverá ser tipo DÉBITO!<br/>";
                        }
                        else if ((view_gc_financeiro_lancamentos.tipo_pag_rec == 2) && (RecordClassificacaoFinanceira.credito == false))
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Classificação Financeira deverá ser tipo CRÉDITO!<br/>";
                        }
                    }
                    if (view_gc_financeiro_lancamentos.valor_pago < 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [R$ Pago/Recebido] não deve ser negativo!<br/>";
                    }
                    if ((view_gc_financeiro_lancamentos.id_financeiro_status == 1) && (view_gc_financeiro_lancamentos.valor_pago <= 0)) // Liquidado
                    {
                        if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento == 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - [R$ Pago/Recebido] deve ser MAIOR que zero! Se houve adiantamento de valores deve-se informar o lançamento de adiantamento na aba (Complemento)<br/>";
                        }
                    }
                    if ((view_gc_financeiro_lancamentos.id_financeiro_status > 2) && (view_gc_financeiro_lancamentos.valor_pago > 0)) // Aberto | Cancelado
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [R$ Pago/Recebido] não deve ser informado para lançamentos com status (Abertos/Cancelados)<br/>";
                    }
                    if ((view_gc_financeiro_lancamentos.tag_provisao_imposto_normal == true) || (view_gc_financeiro_lancamentos.tag_provisao_imposto_reduzido == true))
                    {
                        if (view_gc_financeiro_lancamentos.id_lancamento_tributos > 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Já existe lançamentos de [Provisão de Impostos] para esse lançamento<br/>";
                        }
                        if (view_gc_financeiro_lancamentos.tipo_pag_rec == 1)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Lançamentos de [Provisão de Impostos] somente poderá ser utilizado em lançamentos de crédito<br/>";
                        }
                        if (view_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString().Length == 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Para lançamentos de [Provisão de Impostos] deverá ser informado o Número do Documento/NF<br/>";
                        }
                        if (view_gc_financeiro_lancamentos.id_financeiro_status != 1)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Lançamentos de [Provisão de Impostos] somente poderá ser utilizado em lançamentos liquidados<br/>";
                        }
                    }
                    if ((view_gc_financeiro_lancamentos.tag_provisao_imposto_normal == true) && (view_gc_financeiro_lancamentos.tag_provisao_imposto_reduzido == true))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Deverá ser selecionado apenas uma opção de provisão de impostos<br/>";
                    }
                    if ((view_gc_financeiro_lancamentos.id_lancamento_adiantamento == 0) && ((view_gc_financeiro_lancamentos.id_lancamento_adiantamento2 > 0) || (view_gc_financeiro_lancamentos.id_lancamento_adiantamento3 > 0)))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Os adiantamentos deverão ser informados na ordem correta!<br/>";
                    }
                    if ((view_gc_financeiro_lancamentos.id_lancamento_adiantamento > 0) && (view_gc_financeiro_lancamentos.id_lancamento_adiantamento == view_gc_financeiro_lancamentos.id_lancamento_adiantamento2 ))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Os adiantamentos 1 e 2 não podem ser idênticos!<br/>";
                    }
                    if ((view_gc_financeiro_lancamentos.id_lancamento_adiantamento > 0) && (view_gc_financeiro_lancamentos.id_lancamento_adiantamento == view_gc_financeiro_lancamentos.id_lancamento_adiantamento3))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Os adiantamentos 1 e 3 não podem ser idênticos!<br/>";
                    }
                    if ((view_gc_financeiro_lancamentos.id_lancamento_adiantamento2 > 0) && (view_gc_financeiro_lancamentos.id_lancamento_adiantamento2 == view_gc_financeiro_lancamentos.id_lancamento_adiantamento3))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Os adiantamentos 2 e 3 não podem ser idênticos!<br/>";
                    }
                    if ((view_gc_financeiro_lancamentos.tipo_pag_rec == 1) && (view_gc_financeiro_lancamentos.is_adiantamento == true))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Lançamentos à pagar não podem ser marcados como Adiantamento!<br/>";
                    }

                    if ((view_gc_financeiro_lancamentos.negociacao == true) && (view_gc_financeiro_lancamentos.negociacao_data_limite > DataAtual.AddDays(30)))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Data limite da negociação não poderá ser superior a 30 dias!<br/>";
                    }
                    if (view_gc_financeiro_lancamentos.linha_digitavel_pagamento.EmptyIfNull().ToString().Length > 0)
                    {
                        view_gc_financeiro_lancamentos.linha_digitavel_pagamento = LibStringFormat.SomenteNumeros(view_gc_financeiro_lancamentos.linha_digitavel_pagamento);
                        if ((view_gc_financeiro_lancamentos.linha_digitavel_pagamento.EmptyIfNull().ToString().Length < 47) || (view_gc_financeiro_lancamentos.linha_digitavel_pagamento.EmptyIfNull().ToString().Length > 48))
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Linha digitável inválida, deverá conter 47 ou 48 dígitos numéricos!<br/>";
                        }
                    }
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }

                if (qtdInconsistencias == 0)
                {
                    gc_financeiro_lancamentos record_gc_financeiro_lancamentos = new Db.gc_financeiro_lancamentos();

                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                    if (view_gc_financeiro_lancamentos.id_lancamento == 0) // NOVO LANÇAMENTO FINANCEIRO
                    {
                        record_gc_financeiro_lancamentos = LibDB.CloneTObject(view_gc_financeiro_lancamentos);
                        if (record_gc_financeiro_lancamentos.ordem_pagamento <= 1)
                        {
                            record_gc_financeiro_lancamentos.ordem_pagamento = LibDB.GetNextGcLancamentosFinanceiroOrdemPagamento(record_gc_financeiro_lancamentos.id_conta_caixa, record_gc_financeiro_lancamentos.data_pagamento, db);
                        }
                        record_gc_financeiro_lancamentos.datahora_cadastro = DataHoraAtual;
                        record_gc_financeiro_lancamentos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        db.gc_financeiro_lancamentos.Add(record_gc_financeiro_lancamentos);
                        db.SaveChanges();
                        msgRetorno += "<b>Lançamento " + record_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToLower() + " REGISTRADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                    }
                    else if (view_gc_financeiro_lancamentos.id_lancamento > 0) // EDIÇÃO DE LANÇAMENTO FINANCEIRO
                    {
                        record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(view_gc_financeiro_lancamentos.id_lancamento);
                        record_gc_financeiro_lancamentos.id_tablerow_color = view_gc_financeiro_lancamentos.id_tablerow_color;
                        record_gc_financeiro_lancamentos.id_conta_caixa = view_gc_financeiro_lancamentos.id_conta_caixa;
                        record_gc_financeiro_lancamentos.id_movimento = view_gc_financeiro_lancamentos.id_movimento;
                        record_gc_financeiro_lancamentos.id_movimento_nf = view_gc_financeiro_lancamentos.id_movimento_nf;
                        record_gc_financeiro_lancamentos.gerencial = view_gc_financeiro_lancamentos.gerencial;
                        record_gc_financeiro_lancamentos.negociacao = view_gc_financeiro_lancamentos.negociacao;
                        record_gc_financeiro_lancamentos.negociacao_data_limite = view_gc_financeiro_lancamentos.negociacao_data_limite;
                        record_gc_financeiro_lancamentos.is_adiantamento = view_gc_financeiro_lancamentos.is_adiantamento;
                        record_gc_financeiro_lancamentos.tipo_pag_rec = view_gc_financeiro_lancamentos.tipo_pag_rec;
                        record_gc_financeiro_lancamentos.id_financeiro_status = view_gc_financeiro_lancamentos.id_financeiro_status;
                        record_gc_financeiro_lancamentos.id_lancamento_tributos = view_gc_financeiro_lancamentos.id_lancamento_tributos;
                        record_gc_financeiro_lancamentos.id_pag_rec_tipo = view_gc_financeiro_lancamentos.id_pag_rec_tipo;
                        record_gc_financeiro_lancamentos.parcela_atual = view_gc_financeiro_lancamentos.parcela_atual;
                        record_gc_financeiro_lancamentos.parcela_total = view_gc_financeiro_lancamentos.parcela_total;
                        record_gc_financeiro_lancamentos.fixo = view_gc_financeiro_lancamentos.fixo;
                        if (record_gc_financeiro_lancamentos.data_pagamento.ToString("yyyy-MM-dd") != view_gc_financeiro_lancamentos.data_pagamento.ToString("yyyy-MM-dd")) { record_gc_financeiro_lancamentos.ordem_pagamento = LibDB.GetNextGcLancamentosFinanceiroOrdemPagamento(view_gc_financeiro_lancamentos.id_conta_caixa, view_gc_financeiro_lancamentos.data_pagamento, db); }
                        else { record_gc_financeiro_lancamentos.ordem_pagamento = view_gc_financeiro_lancamentos.ordem_pagamento; }
                        record_gc_financeiro_lancamentos.data_pagamento = view_gc_financeiro_lancamentos.data_pagamento;
                        record_gc_financeiro_lancamentos.data_vencimento = view_gc_financeiro_lancamentos.data_vencimento;
                        record_gc_financeiro_lancamentos.id_cliente = view_gc_financeiro_lancamentos.id_cliente;
                        record_gc_financeiro_lancamentos.descricao = view_gc_financeiro_lancamentos.descricao;
                        record_gc_financeiro_lancamentos.numero_documento = view_gc_financeiro_lancamentos.numero_documento;
                        record_gc_financeiro_lancamentos.valor_pago = view_gc_financeiro_lancamentos.valor_pago;
                        record_gc_financeiro_lancamentos.valor_total = view_gc_financeiro_lancamentos.valor_total;
                        record_gc_financeiro_lancamentos.valor_multa = view_gc_financeiro_lancamentos.valor_multa;
                        record_gc_financeiro_lancamentos.valor_juros = view_gc_financeiro_lancamentos.valor_juros;
                        record_gc_financeiro_lancamentos.valor_descontos = view_gc_financeiro_lancamentos.valor_descontos;
                        record_gc_financeiro_lancamentos.valor_encargos = view_gc_financeiro_lancamentos.valor_encargos;
                        record_gc_financeiro_lancamentos.id_classificacao_financeira = view_gc_financeiro_lancamentos.id_classificacao_financeira;

                        if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento > 0) { record_gc_financeiro_lancamentos.id_lancamento_adiantamento = view_gc_financeiro_lancamentos.id_lancamento_adiantamento; };
                        if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento2 > 0) { record_gc_financeiro_lancamentos.id_lancamento_adiantamento2 = view_gc_financeiro_lancamentos.id_lancamento_adiantamento2; };
                        if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento3 > 0) { record_gc_financeiro_lancamentos.id_lancamento_adiantamento3 = view_gc_financeiro_lancamentos.id_lancamento_adiantamento3; };
                        if (view_gc_financeiro_lancamentos.id_financeiro_status == 3)
                        {
                            if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento > 0) { record_gc_financeiro_lancamentos.id_lancamento_adiantamento = 0; };
                            if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento2 > 0) { record_gc_financeiro_lancamentos.id_lancamento_adiantamento2 = 0; };
                            if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento3 > 0) { record_gc_financeiro_lancamentos.id_lancamento_adiantamento3 = 0; };
                        }
                        record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                        record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                        record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;
                        db.SaveChanges();
                        msgRetorno += "<b>Lançamento " + view_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToLower() + " ALTERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";

                        if (IdMovimentoRelacionado > 0) // Existe um movimento relacionado a esse lançamento
                        {
                            String LogAudit = "Atualização Dados | Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString() + " | ";
                            LogAudit += LibDB.CompareDataTable(record_old_gc_financeiro_lancamentos, record_gc_financeiro_lancamentos);
                            if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true,"gc_movimentos", IdMovimentoRelacionado, LogAudit); };
                        } 
                    }

                    if ((view_gc_financeiro_lancamentos.tag_provisao_imposto_normal == true) || (view_gc_financeiro_lancamentos.tag_provisao_imposto_reduzido == true)) // Se tem previsão de impostos
                    {
                        // Lançamento Débito
                        gc_financeiro_lancamentos RecordDebito = LibDB.CloneTObject(record_gc_financeiro_lancamentos);
                        Decimal ValorPago = RecordDebito.valor_pago;
                        if (view_gc_financeiro_lancamentos.id_lancamento_adiantamento > 0) { ValorPago = RecordDebito.valor_total; };
                        RecordDebito.tipo_pag_rec = 1; // Pagar
                        RecordDebito.gerencial = true;
                        RecordDebito.is_provisao_imposto = true;
                        RecordDebito.ordem_pagamento += 10;
                        if (view_gc_financeiro_lancamentos.tag_provisao_imposto_normal == true)
                        {
                            RecordDebito.valor_pago = ((ValorPago / 100) * 19);
                            RecordDebito.valor_total = RecordDebito.valor_pago;
                            RecordDebito.descricao = "PROVISÃO IMPOSTOS (N) - DÉBITO";
                        }
                        else if (view_gc_financeiro_lancamentos.tag_provisao_imposto_reduzido == true)
                        {
                            RecordDebito.valor_pago = ((ValorPago / 100) * 11);
                            RecordDebito.valor_total = RecordDebito.valor_pago;
                            RecordDebito.descricao = "PROVISÃO IMPOSTOS (R) - DÉBITO";
                        }
                        RecordDebito.id_classificacao_financeira = 68; // DESPESAS TRANSFERÊNCIAS CONTA CAIXA
                        db.gc_financeiro_lancamentos.Add(RecordDebito);
                        msgRetorno += "<b>Provisão de impostos - Débito</b> REGISTRADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";


                        // Lançamento Crédito
                        gc_financeiro_lancamentos RecordCredito = LibDB.CloneTObject(RecordDebito);
                        RecordCredito.tipo_pag_rec = 2; // Receber
                        RecordCredito.ordem_pagamento += 10;
                            if ((view_gc_financeiro_lancamentos.id_conta_caixa == 2) || (view_gc_financeiro_lancamentos.id_conta_caixa == 4)) { RecordCredito.id_conta_caixa = 3; }             // GDI BH -IMPOSTOS
                            else if ((view_gc_financeiro_lancamentos.id_conta_caixa == 11) || (view_gc_financeiro_lancamentos.id_conta_caixa == 13)) { RecordCredito.id_conta_caixa = 12; }     // GDI SP -IMPOSTOS
                        RecordCredito.descricao = RecordCredito.descricao.Replace("DÉBITO", "CRÉDITO");
                        RecordDebito.id_classificacao_financeira = 66;   // RECEITAS TRANSFERÊNCIAS CONTA CAIXA
                        db.gc_financeiro_lancamentos.Add(RecordCredito);
                        msgRetorno += "<b>Provisão de impostos - Crédito</b> REGISTRADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                        db.SaveChanges();

                        // Atualizar o lançamento de tributos
                        record_gc_financeiro_lancamentos.id_lancamento_tributos = RecordDebito.id_lancamento;
                        record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                        record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    sucesso = true;
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
        public void AtualizarSaldoContaCaixa(int IdContaCaixa, DateTime DataReferencia)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DataReferencia = LibDateTime.getPrimeiroDiaMesReferencia(DataReferencia);
            bool NeedUpdate = false;
            String SentencaSql = string.Empty;
            decimal SaldoContaCaixa = 0;
            int Posicao = 0;
            List<gc_financeiro_lancamentos> ListaLancamentos = new List<gc_financeiro_lancamentos>();
            SentencaSql += " select * from gc_financeiro_lancamentos l ";
            SentencaSql += " where ativo = 1 and id_conta_caixa = " + IdContaCaixa.ToString();
            SentencaSql += " and l.data_pagamento >= '" + DataReferencia.ToString("yyyy-MM-dd") + " 00:00:00'"; ;
            //SentencaSql += " order by l.data_pagamento, l.ordem_pagamento, l.data_vencimento ";
            SentencaSql += " order by COALESCE(l.data_pagamento, l.data_vencimento) DESC, l.ordem_pagamento DESC ";

            ListaLancamentos = db.gc_financeiro_lancamentos.SqlQuery(SentencaSql).ToList();
            foreach (gc_financeiro_lancamentos record_gc_financeiro_lancamentos in ListaLancamentos)
            {
                Posicao += 1;
                if (Posicao == 1)
                {
                    if (record_gc_financeiro_lancamentos.tipo_pag_rec == 1) { SaldoContaCaixa = record_gc_financeiro_lancamentos.valor_pago * -1; }
                    else if (record_gc_financeiro_lancamentos.tipo_pag_rec == 2) { SaldoContaCaixa = record_gc_financeiro_lancamentos.valor_pago; }
                }
                else
                {
                    if (record_gc_financeiro_lancamentos.tipo_pag_rec == 1) { SaldoContaCaixa = SaldoContaCaixa - record_gc_financeiro_lancamentos.valor_pago; }
                    else if (record_gc_financeiro_lancamentos.tipo_pag_rec == 2) { SaldoContaCaixa = SaldoContaCaixa + record_gc_financeiro_lancamentos.valor_pago; }
                }
                if ((record_gc_financeiro_lancamentos.id_financeiro_status <= 2) && (record_gc_financeiro_lancamentos.valor_saldo_conta_caixa != SaldoContaCaixa))
                {
                    record_gc_financeiro_lancamentos.valor_saldo_conta_caixa = SaldoContaCaixa;
                    record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                    record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;
                    NeedUpdate = true;
                }
                if ((record_gc_financeiro_lancamentos.id_financeiro_status > 2) && (record_gc_financeiro_lancamentos.valor_saldo_conta_caixa != 0))
                {
                    record_gc_financeiro_lancamentos.valor_saldo_conta_caixa = 0;
                    record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                    record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;
                    NeedUpdate = true;
                }
            }
            if (NeedUpdate) { db.SaveChanges(); };
        }
        public ActionResult ModalViewFinanceiroMovimentos(int? id)
        {
            try
            {
                int TempId = id.GetValueOrDefault();
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = new Db.gc_financeiro_lancamentos();
                record_gc_financeiro_lancamentos.id_movimento = id.GetValueOrDefault();
                CachePersister.userIdentity.IdGcMovimentoAtivo = id.GetValueOrDefault();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Lançamentos Financeiros do Movimento Nº " + TempId.ToString();
                return View("ModalViewFinanceiroMovimentos", record_gc_financeiro_lancamentos);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroLancamentoController";
                msg += "<br/>" + "ModalViewFinanceiroMovimentos(" + id.ToString() + ")";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        #region Financeiro Movimento
        public ActionResult ModalGerarFinanceiroMovimentos(int? id)
        {
            int timeoutOriginal = Session.Timeout;
            gc_movimentos_financeiros record_gc_movimento_financeiro = null;
            try
            {
                Session.Timeout = 480;
                int IdMovimento = id.GetValueOrDefault();
                int QtdParcelas = 0;
                String TitleModal = string.Empty;
                String ObsFinanceiraCliente = string.Empty;
                String MsgHistorico = string.Empty;
                String MsgBloqueio = string.Empty;
                String MsgAdvertencia = string.Empty;
                Decimal ValorParcelaAdiantamento = 0;
                Decimal ValorTotalRateio = 0;
                Decimal SaldoAdiantamentoReal = 0;
                String _SaldoAdiantamentoReal = "0";
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(IdMovimento);
                g_pagrec_condicoes record_g_pagrec_condicoes = db.g_pagrec_condicoes.Find(RecordMovimento.id_pagrec_condicao);
                g_pagrec_tipos record_g_pagrec_tipos = db.g_pagrec_tipos.Find(record_g_pagrec_condicoes.id_pagrec_tipo);
                g_clientes record_g_clientes = db.g_clientes.Find(RecordMovimento.id_cliente);
                record_gc_movimento_financeiro = db.gc_movimentos_financeiros.Where(f => f.id_movimento == IdMovimento && f.ativo == true).FirstOrDefault();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-credit-card", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Gerar Lançamentos Financeiros - Movimento Nº " + IdMovimento.ToString(); ;
                if (record_g_clientes != null) { ObsFinanceiraCliente = record_g_clientes.obs_financeira.EmptyIfNull().ToString(); };
                if (RecordMovimento != null)
                {
                    if (RecordMovimento.obs.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Pedido: " + RecordMovimento.obs.EmptyIfNull().ToString() + "\r\n"; };
                    if (RecordMovimento.frete_observacoes.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Frete: " + RecordMovimento.frete_observacoes.EmptyIfNull().ToString() + "\r\n"; };
                    if (RecordMovimento.obs_aprovacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Aprovação: " + RecordMovimento.obs_aprovacao.EmptyIfNull().ToString() + "\r\n"; };
                    if (RecordMovimento.obs_separacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Separação: " + RecordMovimento.obs_separacao.EmptyIfNull().ToString() + "\r\n"; };
                };

                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);
                if (record_gc_movimento_financeiro != null)
                {
                    MsgBloqueio += "<b> ---------- PEDIDO JÁ FATURADO ANTERIORMENTE ----------</b>" + "<br/>";
                }
                else if (RecordCfopOperacao.has_financeiro == false)
                {
                    MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO [" + RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI ATIVIDADE DE GERAR FINANCEIRO ----------</b>" + "<br/>";
                }
                else if (record_gc_movimento_financeiro == null)
                {
                    if ((RecordCfopOperacao.has_aprovacao == true) && (RecordMovimento.movimento_aprovado == false))  { MsgBloqueio += " - Pedido não foi APROVADO!<br/>"; };
                    if ((RecordCfopOperacao.has_separacao == true) && (RecordMovimento.movimento_separado == false) && (RecordCfopOperacao.permite_faturamento_sem_separacao == false)) { MsgBloqueio += " - Pedido não foi SEPARADO!<br/>"; };
                    if ((RecordCfopOperacao.has_financeiro == true) && (RecordMovimento.movimento_faturado == true)) { MsgBloqueio += " - Pedido já foi FATURADO!<br/>"; };
                    //if ((RecordCfopOperacao.has_nfe == true) && (RecordMovimento.movimento_nf_autorizada == true)) { MsgBloqueio += " - Pedido já possui NFe Autorizada!<br/>"; }
                    //if ((RecordCfopOperacao.has_notifica_email == true) && (RecordMovimento.movimento_notificado == true)) { MsgBloqueio += " - Pedido já foi NOTIFICADO!<br/>"; }
                    //if ((RecordCfopOperacao.has_expedicao == true) && (RecordMovimento.movimento_expedido == true)) { MsgBloqueio += " - Pedido já foi EXPEDIDO!<br/>"; }
                    //if ((RecordCfopOperacao.has_entrega == true) && (RecordMovimento.movimento_entregue == true)) { MsgBloqueio += " - Pedido já foi ENTREGUE!<br/>"; }

                    if (MsgBloqueio.EmptyIfNull().ToString().Length == 0) 
                    {
                        record_gc_movimento_financeiro = new Db.gc_movimentos_financeiros();
                        record_gc_movimento_financeiro.id_movimento = IdMovimento;
                        if (RecordMovimento.id_pagrec_condicao > 0)
                        {
                            String TextoSQL = "select count(*) from gc_financeiro_lancamentos where ativo = 1 and tipo_pag_rec = 2 and id_movimento = " + IdMovimento + " and id_financeiro_status <= 3";
                            if (LibDB.dbQueryCount(TextoSQL, db) == 0) // Não há lançamentos financeiros
                            {
                                g_pagrec_condicoes record_g_pagrec_condicao = db.g_pagrec_condicoes.Find(RecordMovimento.id_pagrec_condicao);
                                Decimal ValorTotalMovimento = RecordMovimento.valor_total_bruto;
                                Decimal ValorAdiantamentoInformado = RecordMovimento.valor_total_adiantamento;
                                Decimal ValorParcelas = (ValorTotalMovimento / record_g_pagrec_condicao.qtd_parcelas);
                                ValorTotalRateio = 0;
                                ValorParcelaAdiantamento = 0;
                                QtdParcelas = record_g_pagrec_condicao.qtd_parcelas;

                                if ((RecordMovimento.id_pagrec_condicao == 2) || (RecordMovimento.valor_total_adiantamento > 0))
                                {
                                    SaldoAdiantamentoReal = 0;
                                    String SentencaSQL = " select sum(valor_total) from gc_financeiro_lancamentos where ativo = 1 and is_adiantamento = 1 and tipo_pag_rec = 2 and id_financeiro_status = 1 and id_cliente = " + RecordMovimento.id_cliente.ToString();
                                    _SaldoAdiantamentoReal = LibDB.dbQueryValue(SentencaSQL, db);
                                    Decimal.TryParse(_SaldoAdiantamentoReal, out SaldoAdiantamentoReal);

                                    if (RecordMovimento.id_pagrec_condicao == 2)
                                    {
                                        if (SaldoAdiantamentoReal == 0)
                                        {
                                            MsgAdvertencia += "<b> ---------- NÃO HÁ SALDO DE ADIANTAMENTO ----------</b><br/>";
                                            MsgAdvertencia += "Não foi encontrado Saldo de Adiantamento para faturar o pedido (A Vista/Antecipado)!";
                                            MsgAdvertencia += "O Pedido não será faturado nas condições de pagamento (Antecipado ou A Vista)";

                                        }
                                        else if ((SaldoAdiantamentoReal > 0) && (SaldoAdiantamentoReal < RecordMovimento.valor_total_bruto))
                                        {
                                            MsgAdvertencia += "<b> ---------- SALDO DE ADIANTAMENTO INSUFICIENTE ----------</b><br/>";
                                            MsgAdvertencia += "O Saldo de adiantamento do cliente [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "] é insuficiente para faturar o pedido " + RecordMovimento.valor_total_bruto.ToString("###,###,###,##0.00") + "<br/>";
                                            MsgAdvertencia += "O Pedido não será faturado nas condições de pagamento (Antecipado ou A Vista)";
                                        }
                                    }
                                    else if ((ValorAdiantamentoInformado > 0) && (ValorAdiantamentoInformado > SaldoAdiantamentoReal))
                                    {
                                        MsgAdvertencia += "<b> ---------- SALDO DE ADIANTAMENTO MENOR QUE ADIANTAMENTO INFORMADO ----------</b><br/>";
                                        MsgAdvertencia += "O Valor do adiantamento informado no pedido [ " + ValorAdiantamentoInformado.ToString("###,###,###,##0.00") + " ], é maior do que o saldo de adiantamento encontrado  [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "]" + "<br/>";
                                        MsgAdvertencia += "O Pedido não será faturado nas condições de pagamento (Antecipado ou A Vista)";
                                    }
                                }

                                if ((ValorAdiantamentoInformado > 0) && (SaldoAdiantamentoReal >= ValorAdiantamentoInformado))
                                {
                                    if (SaldoAdiantamentoReal >= ValorAdiantamentoInformado)
                                    {
                                        if (ValorAdiantamentoInformado < ValorTotalMovimento)
                                        {
                                            ValorParcelaAdiantamento = ValorAdiantamentoInformado;
                                            QtdParcelas += 1;
                                            ValorParcelas = ((ValorTotalMovimento - ValorAdiantamentoInformado) / (record_g_pagrec_condicao.qtd_parcelas));
                                            if (MsgHistorico.EmptyIfNull().Trim().Length > 0) { MsgHistorico += "<br/>"; }; MsgHistorico += " - Valor do Adiantamento a ser utilizado é MENOR que o valor do pedido: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorAdiantamentoInformado);
                                        }
                                        else if (ValorAdiantamentoInformado == ValorTotalMovimento)
                                        {
                                            ValorParcelaAdiantamento = ValorTotalMovimento;
                                            ValorParcelas = 0;
                                            QtdParcelas = 1;
                                            if (MsgHistorico.EmptyIfNull().Trim().Length > 0) { MsgHistorico += "<br/>"; }; MsgHistorico += " - Valor do Adiantamento a ser utilizado é IGUAL ao valor do pedido: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorAdiantamentoInformado);
                                        }
                                        else if (ValorAdiantamentoInformado > ValorTotalMovimento)
                                        {
                                            ValorParcelaAdiantamento = ValorTotalMovimento;
                                            ValorParcelas = 0;
                                            QtdParcelas = 1;
                                            if (MsgHistorico.EmptyIfNull().Trim().Length > 0) { MsgHistorico += "<br/>"; }; MsgHistorico += " - Valor do Adiantamento informado é MAIOR que o valor do pedido: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorAdiantamentoInformado);
                                        }
                                    }
                                }

                                for (int parcela = 1; parcela <= QtdParcelas; parcela++)
                                {
                                    if (parcela == 1)
                                    {
                                        if (ValorParcelaAdiantamento > 0)
                                        {
                                            record_gc_movimento_financeiro.id_pagrec_tipo_1 = 2;
                                            record_gc_movimento_financeiro.data_vencimento_1 = DateTime.Now;
                                            record_gc_movimento_financeiro.valor_total_1 = LibNumbers.TruncateDecimal(ValorParcelaAdiantamento, 2);
                                            ValorTotalRateio += record_gc_movimento_financeiro.valor_total_1;
                                        }
                                        else
                                        {
                                            record_gc_movimento_financeiro.id_pagrec_tipo_1 = record_g_pagrec_tipos.id_pagrec_tipo;
                                            record_gc_movimento_financeiro.data_vencimento_1 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela01);
                                            record_gc_movimento_financeiro.valor_total_1 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                            ValorTotalRateio += record_gc_movimento_financeiro.valor_total_1;
                                        }
                                    }
                                    else if (parcela == 2)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_2 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.valor_total_2 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela02);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_2;
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_2 += (ValorTotalMovimento - ValorTotalRateio); }; // Saldo restante do valor do movimento
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela01); };
                                    }
                                    else if (parcela == 3)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_3 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_3 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela03);
                                        record_gc_movimento_financeiro.valor_total_3 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_3;
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_3 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_3 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela02); };
                                    }
                                    else if (parcela == 4)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_4 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_4 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela04);
                                        record_gc_movimento_financeiro.valor_total_4 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_4;
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_4 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela03); };
                                    }
                                    else if (parcela == 5)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_5 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_5 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela05);
                                        record_gc_movimento_financeiro.valor_total_5 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_5;
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_5 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela04); };
                                    }
                                    else if (parcela == 6)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_6 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_6 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela06);
                                        record_gc_movimento_financeiro.valor_total_6 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_6;
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_6 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela05); };
                                    }
                                    else if (parcela == 7)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_7 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_7 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela07);
                                        record_gc_movimento_financeiro.valor_total_7 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_7;
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_7 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela06); };
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_7 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela06); };
                                    }
                                    else if (parcela == 8)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_8 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_8 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela08);
                                        record_gc_movimento_financeiro.valor_total_8 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_8;
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_8 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela07); };
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_8 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela07); };
                                    }
                                    else if (parcela == 9)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_9 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_9 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela09);
                                        record_gc_movimento_financeiro.valor_total_9 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_9;
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_9 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela08); };
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_9 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela08); };
                                    }
                                    else if (parcela == 10)
                                    {
                                        record_gc_movimento_financeiro.id_pagrec_tipo_10 = record_g_pagrec_tipos.id_pagrec_tipo;
                                        record_gc_movimento_financeiro.data_vencimento_10 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela10);
                                        record_gc_movimento_financeiro.valor_total_10 = LibNumbers.TruncateDecimal(ValorParcelas, 2);
                                        ValorTotalRateio += record_gc_movimento_financeiro.valor_total_10;
                                        if ((parcela == QtdParcelas) && (ValorTotalRateio < ValorTotalMovimento)) { record_gc_movimento_financeiro.valor_total_10 += (ValorTotalMovimento - ValorTotalRateio); };
                                        if (ValorParcelaAdiantamento > 0) { record_gc_movimento_financeiro.data_vencimento_2 = DateTime.Now.AddDays(record_g_pagrec_condicao.dias_parcela09); };
                                    }
                                }
                            }
                        }
                    }
                }
                ViewBag.MsgBloqueio = MsgBloqueio;
                ViewBag.ObsFinanceiraCliente = ObsFinanceiraCliente;
                ViewBag.MsgHistorico = MsgHistorico;
                ViewBag.MsgAdvertencia = MsgAdvertencia;
                PreencherLookupsGerarFinanceiroMovimentos();
                return View("ModalGerarFinanceiroMovimentos", record_gc_movimento_financeiro);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroLancamentoController";
                msg += "<br/>" + "ModalGerarFinanceiroMovimentos(" + id.ToString() + ")";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
            finally
            {
                Session.Timeout = timeoutOriginal;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxModalGerarFinanceiroMovimentos(gc_movimentos_financeiros view_record_gc_movimento_financeiro)
        {
            bool Sucesso = false;
            int QtdInconsistencias = 0;
            int QtdParcelasTotal = 0;
            int QtdLancamentosGerados = 0;
            int idFinanceiroGerado = 0;
            int QtdBoletosACancelar = 0;
            Decimal ValorTotalFinanceiro = 0;
            String msgRetorno = "";
            String LogAlteracoes = "Geração movimento financeiro | ";
            String ListaIdsBoletosCancelar = String.Empty;
            MsgGeral = string.Empty;
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia().Date;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento_financeiro.id_movimento);
            List<g_pagrec_tipos> ListaPagRecTipos = db.g_pagrec_tipos.Where(t => t.id_pagrec_tipo > 0).ToList();
            List<int> ListaIdsFinanceirosGerados = new List<int>();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();


            try
            {
                if (view_record_gc_movimento_financeiro.valor_total_1.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_1 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_2.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_2 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_3.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_3 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_4.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_4 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_5.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_5 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_6.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_6 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_7.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_7 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_8.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_8 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_9.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_9 = 0; };
                if (view_record_gc_movimento_financeiro.valor_total_10.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento_financeiro.valor_total_10 = 0; };

                if (!ModelState.IsValid)
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    QtdInconsistencias += 1;
                }

                // Validar Adiantamento Financeiro para Pedidos à Vista/Antecipado
                if (record_gc_movimento.id_pagrec_condicao == 2)
                {
                    Decimal SaldoAdiantamentoReal = 0;
                    String SentencaSQL = " select sum(valor_total) from gc_financeiro_lancamentos where ativo = 1 and is_adiantamento = 1 and tipo_pag_rec = 2 and id_financeiro_status = 1 and id_cliente = " + record_gc_movimento.id_cliente.ToString();
                    String _SaldoAdiantamentoReal = LibDB.dbQueryValue(SentencaSQL, db);
                    Decimal.TryParse(_SaldoAdiantamentoReal, out SaldoAdiantamentoReal);
                   
                    if (SaldoAdiantamentoReal == 0)
                    {
                        QtdInconsistencias += 1;
                        msgRetorno += " - Não foi encontrado Saldo de Adiantamento para esse Cliente!";
                    }
                    else if ((SaldoAdiantamentoReal > 0) && (SaldoAdiantamentoReal < record_gc_movimento.valor_total_bruto))
                    {
                        QtdInconsistencias += 1;
                        msgRetorno += " - Saldo de adiantamento [ " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", SaldoAdiantamentoReal).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " ] ";
                        msgRetorno += " é menor do que o valor total do pedido [ " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_movimento.valor_total_bruto).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " ] ";
                    }
                }

                if (QtdInconsistencias == 0)
                {
                    if ((view_record_gc_movimento_financeiro.valor_total_1 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_1 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_1 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 1" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_2 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_2 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_2 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 2" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_3 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_3 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_3 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 3" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_4 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_4 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_4 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 4" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_5 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_5 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_5 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 5" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_6 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_6 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_6 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 6" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_7 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_7 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_7 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 7" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_8 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_8 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_8 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 8" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_9 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_9 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_9 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 9" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.valor_total_10 > 0) && ((view_record_gc_movimento_financeiro.id_pagrec_tipo_10 == 0) || (view_record_gc_movimento_financeiro.data_vencimento_10 == null)))
                    {
                        msgRetorno += " - Verifique corretamente os dados da parcela 10" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    
                    if ((view_record_gc_movimento_financeiro.data_vencimento_1 != null) && (view_record_gc_movimento_financeiro.data_vencimento_1.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(1) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_2 != null) && (view_record_gc_movimento_financeiro.data_vencimento_2.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(2) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_3 != null) && (view_record_gc_movimento_financeiro.data_vencimento_3.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(3) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_4 != null) && (view_record_gc_movimento_financeiro.data_vencimento_4.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(4) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_5 != null) && (view_record_gc_movimento_financeiro.data_vencimento_5.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(5) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_6 != null) && (view_record_gc_movimento_financeiro.data_vencimento_6.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(6) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_7 != null) && (view_record_gc_movimento_financeiro.data_vencimento_7.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(7) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_8 != null) && (view_record_gc_movimento_financeiro.data_vencimento_8.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(8) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_9 != null) && (view_record_gc_movimento_financeiro.data_vencimento_9.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(9) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if ((view_record_gc_movimento_financeiro.data_vencimento_10 != null) && (view_record_gc_movimento_financeiro.data_vencimento_10.GetValueOrDefault().Date < DataAtual))
                    {
                        msgRetorno += " - Data Venc.(10) inferior a data atual!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    if (record_gc_movimento == null)
                    {
                        msgRetorno += " - Cotação/Pedido/OS não encontrado" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                    else
                    {
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_1;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_2;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_3;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_4;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_5;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_6;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_7;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_8;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_9;
                        ValorTotalFinanceiro += view_record_gc_movimento_financeiro.valor_total_10;
                        if (ValorTotalFinanceiro < record_gc_movimento.valor_total_bruto)
                        {
                            msgRetorno += " - Valor total dos lançamentos financeiros <b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotalFinanceiro) + "</b><br/>";
                            msgRetorno += "<b>NÃO</b> pode ser menor do que o valor total do pedido <b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_movimento.valor_total_bruto) + "<b>";
                            QtdInconsistencias += 1;
                        }
                        else if (ValorTotalFinanceiro > (record_gc_movimento.valor_total_bruto + 1))
                        {
                            msgRetorno += " - Valor total dos lançamentos financeiros <b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotalFinanceiro) + "</b><br/>";
                            msgRetorno += "<b>NÃO</b> pode ser MAIOR do que o valor total do pedido <b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_movimento.valor_total_bruto) + "<b>";
                            QtdInconsistencias += 1;
                        }

                    }
                }

                if (QtdInconsistencias == 0)
                {
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_1 > 0) && (view_record_gc_movimento_financeiro.valor_total_1 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_2 > 0) && (view_record_gc_movimento_financeiro.valor_total_2 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_3 > 0) && (view_record_gc_movimento_financeiro.valor_total_3 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_4 > 0) && (view_record_gc_movimento_financeiro.valor_total_4 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_5 > 0) && (view_record_gc_movimento_financeiro.valor_total_5 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_6 > 0) && (view_record_gc_movimento_financeiro.valor_total_6 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_7 > 0) && (view_record_gc_movimento_financeiro.valor_total_7 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_8 > 0) && (view_record_gc_movimento_financeiro.valor_total_8 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_9 > 0) && (view_record_gc_movimento_financeiro.valor_total_9 > 0)) { QtdParcelasTotal += 1; }
                    if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_10 > 0) && (view_record_gc_movimento_financeiro.valor_total_10 > 0)) { QtdParcelasTotal += 1; }

                    // Criação dos lançamentos e títulos financeiros
                    if (QtdParcelasTotal > 0)
                    {
                        OrdemPagamento = 0;

                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_1 > 0) && (view_record_gc_movimento_financeiro.valor_total_1 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 1, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_1.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_1.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_1).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }

                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_2 > 0) && (view_record_gc_movimento_financeiro.valor_total_2 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 2, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_2.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_2.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_2).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }

                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_3 > 0) && (view_record_gc_movimento_financeiro.valor_total_3 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 3, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_3.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_3.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_3).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }

                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_4 > 0) && (view_record_gc_movimento_financeiro.valor_total_4 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 4, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_4.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_4.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_4).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }

                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_5 > 0) && (view_record_gc_movimento_financeiro.valor_total_5 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 5, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_5.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_5.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_5).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }

                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_6 > 0) && (view_record_gc_movimento_financeiro.valor_total_6 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 6, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_6.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_6.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_6).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }

                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_7 > 0) && (view_record_gc_movimento_financeiro.valor_total_7 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 7, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_7.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_7.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_7).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }
                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_8 > 0) && (view_record_gc_movimento_financeiro.valor_total_8 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 8, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_8.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_8.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_8).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }
                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_9 > 0) && (view_record_gc_movimento_financeiro.valor_total_9 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 9, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_9.GetValueOrDefault().ToString("dd/MM/yy") + " - ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_9.ToString("N2") + " - ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_9).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }
                        if ((view_record_gc_movimento_financeiro.id_pagrec_tipo_10 > 0) && (view_record_gc_movimento_financeiro.valor_total_10 > 0))
                        {
                            try
                            {
                                idFinanceiroGerado = CreateFinanceiroMovimento(record_gc_movimento, view_record_gc_movimento_financeiro, 10, QtdParcelasTotal);
                                QtdLancamentosGerados += 1;
                                LogAlteracoes += "Id: " + idFinanceiroGerado.ToString() + ", ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.data_vencimento_10.GetValueOrDefault().ToString("dd/MM/yy") + ", ";
                                LogAlteracoes += view_record_gc_movimento_financeiro.valor_total_10.ToString("N2") + ", ";
                                LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == view_record_gc_movimento_financeiro.id_pagrec_tipo_10).descricao.EmptyIfNull().ToString() + " | ";
                                if (idFinanceiroGerado > 0) { ListaIdsFinanceirosGerados.Add(idFinanceiroGerado); };
                            }
                            catch (Exception) { QtdInconsistencias += 1; }
                        }

                        if ((QtdLancamentosGerados > 0) && (QtdInconsistencias == 0))
                        {
                            msgRetorno = "Movimento Nº " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>Faturado</b>" + LibStringFormat.GetTabHtml(1) + "[ " + QtdParcelasTotal.ToString() + " parcela(s)]" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                            msgRetorno += "<br/><br/>" + "<b>----- Lançamentos Financeiros Gerados com Sucesso -----</b>" + "<br/>" + MsgGeral;

                            record_gc_movimento.id_movimento_tipo = 4; // Pedido
                            record_gc_movimento.movimento_faturado = true;
                            if (record_gc_movimento.id_movimento_status < 2) { record_gc_movimento.id_movimento_status = 2; } // Fechado
                            if (record_gc_movimento.id_movimento_posicao < 3) { record_gc_movimento.id_movimento_posicao = 3; } // // Faturado
                            record_gc_movimento.id_usuario_faturamento = CachePersister.userIdentity.IdUsuario;
                            record_gc_movimento.datahora_faturamento = LibDateTime.getDataHoraBrasilia();
                            record_gc_movimento.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento).State = EntityState.Modified;

                            view_record_gc_movimento_financeiro.movimento_faturado = true;
                            view_record_gc_movimento_financeiro.ativo = true;
                            db.gc_movimentos_financeiros.Add(view_record_gc_movimento_financeiro);

                            db.SaveChanges();
                            Sucesso = true;

                            if (Sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); }; };
                        }
                        else
                        {
                            // Desabilitar os lançamentos financeiros errados/orfãos
                            try
                            {
                                if (ListaIdsFinanceirosGerados.Count > 0)
                                {
                                    foreach (int IdFinanceiroLancamento in ListaIdsFinanceirosGerados)
                                    {
                                        gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(IdFinanceiroLancamento);
                                        record_gc_financeiro_lancamentos.ativo = false;
                                        record_gc_financeiro_lancamentos.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                                        record_gc_financeiro_lancamentos.datahora_cancelamento = DataAtual;
                                        record_gc_financeiro_lancamentos.motivo_cancelamento = "ERP: Erro interno geração lançamentos";
                                        record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                                        record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;

                                        // CRIAR ATENDIMENTO
                                        if (record_gc_financeiro_lancamentos.id_pag_rec_tipo == 3)
                                        {
                                            ListaIdsBoletosCancelar += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ",";
                                            QtdBoletosACancelar += 1;
                                            g_clientes record_g_clientes = db.g_clientes.Find(record_gc_financeiro_lancamentos.id_cliente);

                                            g_atendimentos NovoAtendimento = new g_atendimentos();
                                            NovoAtendimento.concluido = false;
                                            NovoAtendimento.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                                            NovoAtendimento.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                                            NovoAtendimento.privado = false;
                                            NovoAtendimento.enviar_atualizacoes = false;
                                            NovoAtendimento.param_id_cliente = record_gc_financeiro_lancamentos.id_cliente;
                                            NovoAtendimento.param_numero_pedido = record_gc_financeiro_lancamentos.id_movimento;
                                            NovoAtendimento.param_numero_nf = 0;
                                            NovoAtendimento.param_id_produto = -1;
                                            NovoAtendimento.param_limite_credito = 0;
                                            NovoAtendimento.param_id_vendedor = -1;
                                            NovoAtendimento.id_status = 1; // Aberto
                                            NovoAtendimento.id_atendimento_categoria = 1; // Pedidos - Faturamento / Boleto(Alterar / Cancelar)
                                            NovoAtendimento.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                                            NovoAtendimento.solicitacao_datahora = DataHoraAtual;
                                            NovoAtendimento.responsavel_id_usuario = 0;
                                            NovoAtendimento.responsavel_id_departamento = 1;
                                            NovoAtendimento.id_coligada = 0;
                                            NovoAtendimento.id_filial = 0;
                                            NovoAtendimento.datahora_cadastro = DataHoraAtual;
                                            NovoAtendimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                            db.g_atendimentos.Add(NovoAtendimento);
                                            db.SaveChanges();

                                            g_atendimentos_atividades NovaAtividade = new g_atendimentos_atividades();
                                            NovaAtividade.id_atendimento = NovoAtendimento.id_atendimento;
                                            NovaAtividade.id_atendimento_categoria_atividade = 1;
                                            NovaAtividade.concluido = false;
                                            NovaAtividade.privado = false;
                                            NovaAtividade.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                                            NovaAtividade.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                                            NovaAtividade.ordem = 0;
                                            NovaAtividade.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                                            NovaAtividade.solicitacao_datahora = DataHoraAtual;
                                            NovaAtividade.responsavel_id_usuario = 0;
                                            NovaAtividade.responsavel_id_departamento = 1;
                                            NovaAtividade.conclusao_id_usuario = 0;
                                            NovaAtividade.cancelamento_id_usuario = 0;
                                            NovaAtividade.id_coligada = 0;
                                            NovaAtividade.id_filial = 0;
                                            NovaAtividade.datahora_cadastro = DataHoraAtual;
                                            NovaAtividade.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                            db.g_atendimentos_atividades.Add(NovaAtividade);
                                            db.SaveChanges();
                                        }
                                        db.SaveChanges();
                                    }
                                }
                            }
                            catch (Exception) { }
                            msgRetorno += "<br/><br/>" + "<b>----- ERRO na Geração dos Lançamentos Financeiros -----</b>" + "<br/>" + MsgGeral;
                            if (QtdBoletosACancelar > 0) { msgRetorno += QtdBoletosACancelar.ToString() +  " Boletos à Cancelar (" + ListaIdsBoletosCancelar.ToString() + ")" + "<br/>" + MsgGeral; };
                            if (MsgGeral.Length > 0) { msgRetorno += "Mensagem: " + MsgGeral; };
                            Sucesso = false;

                            LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_movimento.id_movimento, msgRetorno.Replace("<br/>"," | ").Replace("-----", "").Replace("</b>", "").Replace("<b>", ""));
                        }
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                // Desabilitar os lançamentos financeiros errados/orfãos
                try
                {
                    if (ListaIdsFinanceirosGerados.Count > 0)
                    {
                        foreach (int IdFinanceiroLancamento in ListaIdsFinanceirosGerados)
                        {
                            gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(IdFinanceiroLancamento);
                            record_gc_financeiro_lancamentos.ativo = false;
                            record_gc_financeiro_lancamentos.motivo_cancelamento = "ERP: Erro interno geração lançamentos";
                            record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                            record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;

                            // CRIAR ATENDIMENTO 
                            if (record_gc_financeiro_lancamentos.id_pag_rec_tipo == 3)
                            {
                                ListaIdsBoletosCancelar += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ",";
                                QtdBoletosACancelar += 1;

                                ListaIdsBoletosCancelar += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ",";
                                QtdBoletosACancelar += 1;
                                g_clientes record_g_clientes = db.g_clientes.Find(record_gc_financeiro_lancamentos.id_cliente);

                                g_atendimentos NovoAtendimento = new g_atendimentos();
                                NovoAtendimento.concluido = false;
                                NovoAtendimento.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                                NovoAtendimento.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                                NovoAtendimento.privado = false;
                                NovoAtendimento.enviar_atualizacoes = false;
                                NovoAtendimento.param_id_cliente = record_gc_financeiro_lancamentos.id_cliente;
                                NovoAtendimento.param_numero_pedido = record_gc_financeiro_lancamentos.id_movimento;
                                NovoAtendimento.param_numero_nf = 0;
                                NovoAtendimento.param_id_produto = -1;
                                NovoAtendimento.param_limite_credito = 0;
                                NovoAtendimento.param_id_vendedor = -1;
                                NovoAtendimento.id_status = 1; // Aberto
                                NovoAtendimento.id_atendimento_categoria = 1; // Pedidos - Faturamento / Boleto(Alterar / Cancelar)
                                NovoAtendimento.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                                NovoAtendimento.solicitacao_datahora = DataHoraAtual;
                                NovoAtendimento.responsavel_id_usuario = 0;
                                NovoAtendimento.responsavel_id_departamento = 1;
                                NovoAtendimento.id_coligada = 0;
                                NovoAtendimento.id_filial = 0;
                                NovoAtendimento.datahora_cadastro = DataHoraAtual;
                                NovoAtendimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                db.g_atendimentos.Add(NovoAtendimento);
                                db.SaveChanges();

                                g_atendimentos_atividades NovaAtividade = new g_atendimentos_atividades();
                                NovaAtividade.id_atendimento = NovoAtendimento.id_atendimento;
                                NovaAtividade.id_atendimento_categoria_atividade = 1;
                                NovaAtividade.concluido = false;
                                NovaAtividade.privado = false;
                                NovaAtividade.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                                NovaAtividade.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                                NovaAtividade.ordem = 0;
                                NovaAtividade.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                                NovaAtividade.solicitacao_datahora = DataHoraAtual;
                                NovaAtividade.responsavel_id_usuario = 0;
                                NovaAtividade.responsavel_id_departamento = 1;
                                NovaAtividade.conclusao_id_usuario = 0;
                                NovaAtividade.cancelamento_id_usuario = 0;
                                NovaAtividade.id_coligada = 0;
                                NovaAtividade.id_filial = 0;
                                NovaAtividade.datahora_cadastro = DataHoraAtual;
                                NovaAtividade.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                db.g_atendimentos_atividades.Add(NovaAtividade);
                                db.SaveChanges();
                            }
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception) { }
                Sucesso = false;
                QtdInconsistencias = 1;
                msgRetorno += "<br/><br/>" + "<b>----- ERRO na Geração dos Lançamentos Financeiros -----</b>" + "<br/>";
                if (QtdBoletosACancelar > 0) { msgRetorno += QtdBoletosACancelar.ToString() + " Boletos à Cancelar (" + ListaIdsBoletosCancelar.ToString() + ")" + "<br/>" + MsgGeral; };
                msgRetorno += "Mensagem: " + GdiMvcJsonResults.AjaxFailureValidationMessage(ex);
                LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_movimento.id_movimento, msgRetorno.Replace("<br/>", " | ").Replace("-----", "").Replace("</b>", "").Replace("<b>", ""));
            }
            catch (Exception e)
            {
                // Desabilitar os lançamentos financeiros errados/orfãos
                try
                {
                    if (ListaIdsFinanceirosGerados.Count > 0)
                    {
                        foreach (int IdFinanceiroLancamento in ListaIdsFinanceirosGerados)
                        {
                            gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(IdFinanceiroLancamento);
                            record_gc_financeiro_lancamentos.ativo = false;
                            record_gc_financeiro_lancamentos.motivo_cancelamento = "ERP: Erro interno geração lançamentos";
                            record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                            record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;

                            // CRIAR ATENDIMENTO 
                            if (record_gc_financeiro_lancamentos.id_pag_rec_tipo == 3)
                            {
                                ListaIdsBoletosCancelar += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ",";
                                QtdBoletosACancelar += 1;

                                ListaIdsBoletosCancelar += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ",";
                                QtdBoletosACancelar += 1;
                                g_clientes record_g_clientes = db.g_clientes.Find(record_gc_financeiro_lancamentos.id_cliente);

                                g_atendimentos NovoAtendimento = new g_atendimentos();
                                NovoAtendimento.concluido = false;
                                NovoAtendimento.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                                NovoAtendimento.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                                NovoAtendimento.privado = false;
                                NovoAtendimento.enviar_atualizacoes = false;
                                NovoAtendimento.param_id_cliente = record_gc_financeiro_lancamentos.id_cliente;
                                NovoAtendimento.param_numero_pedido = record_gc_financeiro_lancamentos.id_movimento;
                                NovoAtendimento.param_numero_nf = 0;
                                NovoAtendimento.param_id_produto = -1;
                                NovoAtendimento.param_limite_credito = 0;
                                NovoAtendimento.param_id_vendedor = -1;
                                NovoAtendimento.id_status = 1; // Aberto
                                NovoAtendimento.id_atendimento_categoria = 1; // Pedidos - Faturamento / Boleto(Alterar / Cancelar)
                                NovoAtendimento.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                                NovoAtendimento.solicitacao_datahora = DataHoraAtual;
                                NovoAtendimento.responsavel_id_usuario = 0;
                                NovoAtendimento.responsavel_id_departamento = 1;
                                NovoAtendimento.id_coligada = 0;
                                NovoAtendimento.id_filial = 0;
                                NovoAtendimento.datahora_cadastro = DataHoraAtual;
                                NovoAtendimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                db.g_atendimentos.Add(NovoAtendimento);
                                db.SaveChanges();

                                g_atendimentos_atividades NovaAtividade = new g_atendimentos_atividades();
                                NovaAtividade.id_atendimento = NovoAtendimento.id_atendimento;
                                NovaAtividade.id_atendimento_categoria_atividade = 1;
                                NovaAtividade.concluido = false;
                                NovaAtividade.privado = false;
                                NovaAtividade.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                                NovaAtividade.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                                NovaAtividade.ordem = 0;
                                NovaAtividade.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                                NovaAtividade.solicitacao_datahora = DataHoraAtual;
                                NovaAtividade.responsavel_id_usuario = 0;
                                NovaAtividade.responsavel_id_departamento = 1;
                                NovaAtividade.conclusao_id_usuario = 0;
                                NovaAtividade.cancelamento_id_usuario = 0;
                                NovaAtividade.id_coligada = 0;
                                NovaAtividade.id_filial = 0;
                                NovaAtividade.datahora_cadastro = DataHoraAtual;
                                NovaAtividade.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                db.g_atendimentos_atividades.Add(NovaAtividade);
                                db.SaveChanges();
                            }
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception) { }
                Sucesso = false;
                QtdInconsistencias = 1;
                msgRetorno += "<br/><br/>" + "<b>----- ERRO na Geração dos Lançamentos Financeiros -----</b>" + "<br/>";
                if (QtdBoletosACancelar > 0) { msgRetorno += QtdBoletosACancelar.ToString() + " Boletos à Cancelar (" + ListaIdsBoletosCancelar.ToString() + ")" + "<br/>" + MsgGeral; };
                msgRetorno += "Mensagem: " + GdiMvcJsonResults.AjaxFailureMessage(e);
                LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_movimento.id_movimento, msgRetorno.Replace("<br/>", " | ").Replace("-----", "").Replace("</b>", "").Replace("<b>", ""));
            }
            return Json(new { success = Sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        public int CreateFinanceiroMovimento(gc_movimentos view_record_gc_movimento, gc_movimentos_financeiros view_record_gc_movimento_financeiro, int Parcela, int QtdParcelasTotal)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            gc_financeiro_lancamentos new_record_gc_financeiro_lancamento = new gc_financeiro_lancamentos();
            try
            {
                int IdFinanceiroGateway = 1; // 1 - Bradesco | 2 - Itaú
                bool AmbienteProducao = true;
                gc_parametros record_gc_parametros = db.gc_parametros.Find(1);
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento); // Movimento
                g_financeiro_gateway record_g_financeiro_gateway = new Db.g_financeiro_gateway();
                if (record_gc_parametros != null)
                {
                    if (record_gc_parametros.id_financeiro_gateway > 0) { IdFinanceiroGateway = record_gc_parametros.id_financeiro_gateway; };
                    record_g_financeiro_gateway = db.g_financeiro_gateway.Find(IdFinanceiroGateway);
                    if (record_g_financeiro_gateway != null) { AmbienteProducao = record_g_financeiro_gateway.producao; }
                }

                DateTime DataVencimentoParcela = DataHoraAtual;
                Decimal ValorTotalParcela = 0;
                int TipoPagamentoParcela = 0;
                if (OrdemPagamento == 0) { OrdemPagamento = LibDB.GetNextGcLancamentosFinanceiroOrdemPagamento(2, DataHoraAtual, db); } else { OrdemPagamento += 10; };

                if (Parcela == 1)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_1.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_1;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_1;
                }
                else if (Parcela == 2)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_2.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_2;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_2;
                }
                else if (Parcela == 3)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_3.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_3;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_3;
                }
                else if (Parcela == 4)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_4.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_4;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_4;
                }
                else if (Parcela == 5)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_5.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_5;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_5;
                }
                else if (Parcela == 6)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_6.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_6;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_6;
                }
                else if (Parcela == 7)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_7.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_7;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_7;
                }
                else if (Parcela == 8)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_8.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_8;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_8;
                }
                else if (Parcela == 9)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_9.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_9;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_9;
                }
                else if (Parcela == 10)
                {
                    DataVencimentoParcela = view_record_gc_movimento_financeiro.data_vencimento_10.GetValueOrDefault();
                    ValorTotalParcela = view_record_gc_movimento_financeiro.valor_total_10;
                    TipoPagamentoParcela = view_record_gc_movimento_financeiro.id_pagrec_tipo_10;
                }

                // Fallback: parcela sem vencimento informado (data_vencimento_N nulo → GetValueOrDefault() = DateTime.MinValue)
                // evita gravar a data-sentinela 0001-01-01 em data_vencimento/data_pagamento (campo não-nullable).
                if (DataVencimentoParcela < new DateTime(1900, 1, 1)) { DataVencimentoParcela = DataHoraAtual; }

                // Gestão Comercial - Lançamento Financeiro
                new_record_gc_financeiro_lancamento.id_lancamento_origem = 2; // Vendas GC
                new_record_gc_financeiro_lancamento.tipo_pag_rec = 2; // Receber
                new_record_gc_financeiro_lancamento.id_pag_rec_tipo = TipoPagamentoParcela;
                new_record_gc_financeiro_lancamento.id_movimento = view_record_gc_movimento_financeiro.id_movimento;
                new_record_gc_financeiro_lancamento.ativo = true;
                new_record_gc_financeiro_lancamento.fixo = false;
                new_record_gc_financeiro_lancamento.gerencial = false;
                new_record_gc_financeiro_lancamento.parcela_atual = Parcela;
                new_record_gc_financeiro_lancamento.parcela_total = QtdParcelasTotal;
                new_record_gc_financeiro_lancamento.id_financeiro_status = 3; // Aberto
                new_record_gc_financeiro_lancamento.id_cliente = view_record_gc_movimento.id_cliente;
                new_record_gc_financeiro_lancamento.data_vencimento = DataVencimentoParcela;
                new_record_gc_financeiro_lancamento.data_vencimento_original = DataVencimentoParcela;
                new_record_gc_financeiro_lancamento.data_pagamento = DataVencimentoParcela;
                new_record_gc_financeiro_lancamento.ordem_pagamento = OrdemPagamento;
                new_record_gc_financeiro_lancamento.descricao = "PEDIDO nº " + view_record_gc_movimento.id_movimento.ToString();

                if (record_gc_movimento.id_local_estoque == 1)
                {
                    if (record_gc_movimento.id_vendedor == 6) { new_record_gc_financeiro_lancamento.id_conta_caixa = 4; } // SC BH - VENDAS
                    else { new_record_gc_financeiro_lancamento.id_conta_caixa = 2; } // GDI BH - VENDAS
                    new_record_gc_financeiro_lancamento.id_filial = 1;
                }
                else if (record_gc_movimento.id_local_estoque == 3)
                {
                    if (record_gc_movimento.id_vendedor == 6) { new_record_gc_financeiro_lancamento.id_conta_caixa = 13; } // SC SP - VENDAS
                    else { new_record_gc_financeiro_lancamento.id_conta_caixa = 11; } // GDI SP - VENDAS
                    new_record_gc_financeiro_lancamento.id_filial = 1;      // MultiColigada - GDI SP
                }

                new_record_gc_financeiro_lancamento.valor_total = ValorTotalParcela;
                new_record_gc_financeiro_lancamento.valor_pago = 0;
                new_record_gc_financeiro_lancamento.valor_saldo_conta_caixa = 0;
                new_record_gc_financeiro_lancamento.id_coligada = 1;
                new_record_gc_financeiro_lancamento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                new_record_gc_financeiro_lancamento.datahora_cadastro = DataHoraAtual;
                new_record_gc_financeiro_lancamento.id_classificacao_financeira = 3;     // RECEITAS DE VENDAS DE PRODUTOS
                db.gc_financeiro_lancamentos.Add(new_record_gc_financeiro_lancamento);
                db.SaveChanges(); // Salvar o lançamento financeiro

                if (Parcela == 1) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_1 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 2) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_2 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 3) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_3 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 4) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_4 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 5) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_5 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 6) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_6 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 7) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_7 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 8) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_8 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 9) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_9 = new_record_gc_financeiro_lancamento.id_lancamento; }
                else if (Parcela == 10) { view_record_gc_movimento_financeiro.id_financeiro_lancamento_10 = new_record_gc_financeiro_lancamento.id_lancamento; }

                MsgGeral += "Parcela " + Parcela.EmptyIfNull().ToString() + "  |  Id: " + new_record_gc_financeiro_lancamento.id_lancamento.ToString() + "  |  Venc: " + DataVencimentoParcela.ToString("dd/MM/yyyy") + "  |  R$: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotalParcela).Replace("R$ ", "").Replace("R$", "").Replace("$", "");

                if (new_record_gc_financeiro_lancamento.id_pag_rec_tipo == 3) // Boleto
                {
                    try
                    {
                        int RetornoBoleto = 0;
                        RetornoBoleto = RegistrarBolecodeItau(new_record_gc_financeiro_lancamento.id_lancamento, IdFinanceiroGateway, AmbienteProducao); // Itaú
                        if (RetornoBoleto > 0)
                        {
                            MsgGeral += "  |  Boleto On-Line Gerado!<br/>";
                        }
                        else
                        {
                            new_record_gc_financeiro_lancamento.ativo = false;
                            new_record_gc_financeiro_lancamento.motivo_cancelamento = "Erro geração boleto on-line";
                            new_record_gc_financeiro_lancamento.datahora_alteracao = DataHoraAtual;
                            new_record_gc_financeiro_lancamento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(new_record_gc_financeiro_lancamento).State = EntityState.Modified;
                            db.SaveChanges();
                            throw new Exception("Erro ao gerar o boleto on-line");
                        }
                    }
                    catch (Exception e)
                    {
                        new_record_gc_financeiro_lancamento.ativo = false;
                        new_record_gc_financeiro_lancamento.motivo_cancelamento = "Erro geração boleto on-line";
                        new_record_gc_financeiro_lancamento.datahora_alteracao = DataHoraAtual;
                        new_record_gc_financeiro_lancamento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(new_record_gc_financeiro_lancamento).State = EntityState.Modified;
                        db.SaveChanges();
                        MsgGeral += "  |  Erro Boleto On-Line [" + GdiMvcJsonResults.AjaxFailureMessage(e) + "]!<br/>";
                        throw e;
                    }
                }
                Thread.Sleep(2000);
                return new_record_gc_financeiro_lancamento.id_lancamento;
            }
            catch (Exception e)
            {
                if (new_record_gc_financeiro_lancamento != null)
                {
                    if (new_record_gc_financeiro_lancamento.id_lancamento > 0)
                    {
                        new_record_gc_financeiro_lancamento.ativo = false;
                        new_record_gc_financeiro_lancamento.motivo_cancelamento = "Erro geração financeiro";
                        new_record_gc_financeiro_lancamento.datahora_alteracao = DataHoraAtual;
                        new_record_gc_financeiro_lancamento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(new_record_gc_financeiro_lancamento).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                MsgGeral += "  |  Erro Boleto On-Line [" + GdiMvcJsonResults.AjaxFailureMessage(e) + "]!<br/>";
                Thread.Sleep(2000);
                throw e;
            }
        }

        public int RegistrarBolecodeItau(int idFinanceiroLancamento, int IdFinanceiroGateway, bool AmbienteProducao)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(idFinanceiroLancamento);
            g_contas_caixas RecordContaCaixaLancamento = db.g_contas_caixas.Find(record_gc_financeiro_lancamentos.id_conta_caixa);
            g_contas_caixas RecordContaCaixaBancaria = db.g_contas_caixas.Find(RecordContaCaixaLancamento.id_conta_bancaria); // Rastrear a conta caixa do lançamento
            //g_contas_caixas RecordContaCaixaBancaria = db.g_contas_caixas.Find(7); // Rastrear a conta caixa do lançamento
            int Resultado = 0;
            string FileNameCertificadoItau = string.Empty;
            try
            {
                if (RecordContaCaixaBancaria.id_conta_caixa == 7) { FileNameCertificadoItau = Path.Combine(Server.MapPath("~/Lib/Certificados"), "certificado-itau-gdibh-2027-02-24.pfx"); }      // GDI BH
                else if (RecordContaCaixaBancaria.id_conta_caixa == 10) { FileNameCertificadoItau = Path.Combine(Server.MapPath("~/Lib/Certificados"), "itau-certificado-gdisp-2027-04-07.pfx"); };  // GDI SP
                //else if (RecordContaCaixaBancaria.id_conta_caixa == 10) { FileNameCertificadoItau = Path.Combine(Server.MapPath("~/Lib/Certificados"), "certificado-itau-gdisp-2026-04-15.pfx"); };  // GDI SP

                if (record_gc_financeiro_lancamentos != null)
                {
                    g_clientes record_g_clientes = db.g_clientes.Find(record_gc_financeiro_lancamentos.id_cliente);
                    DateTime BolecodeDataEmissao = DataHoraAtual;
                    DateTime BolecodeDataVencimento = record_gc_financeiro_lancamentos.data_vencimento;
                    DateTime BolecodeDataLimitePagamento = BolecodeDataVencimento.AddDays(30);
                    Decimal BolecodeValorTitulo = record_gc_financeiro_lancamentos.valor_total;
                    Decimal BolecodeValorMulta = ((BolecodeValorTitulo / 100) * 2);
                    Decimal BolecodeValorJuros = ((BolecodeValorTitulo / 10000) * 34); // 0,34
                    String BolecodeNomePagador = record_g_clientes.nome.EmptyIfNull().ToString().Trim();
                    String BolecodeDocumentoTipo = string.Empty;
                    String BolecodePagadorDocumentoNumero = string.Empty;
                    String BolecodePagadorPagadorEndereco = string.Empty;
                    String BolecodePagadorPagadorBairro = string.Empty;
                    String BolecodePagadorPagadorCidade = string.Empty;
                    String BolecodePagadorPagadorUF = string.Empty;
                    String BolecodePagadorPagadorCEP = string.Empty;
                    int BolecodeNossoNumero = idFinanceiroLancamento;

                    if (record_g_clientes.cpf.EmptyIfNull().Length > 0)
                    {
                        BolecodeDocumentoTipo = "F";
                        BolecodePagadorDocumentoNumero = record_g_clientes.cpf.EmptyIfNull().Replace(" ", "").Replace(" ", "").Replace(".", "").Replace("-", "");
                    }
                    else if (record_g_clientes.cnpj.EmptyIfNull().Length > 0)
                    {
                        BolecodeDocumentoTipo = "J";
                        BolecodePagadorDocumentoNumero = record_g_clientes.cnpj.EmptyIfNull().Replace(" ", "").Replace(" ", "").Replace(".", "").Replace("-", "");
                    }

                    if (BolecodeNomePagador.Length > 50) { BolecodeNomePagador = BolecodeNomePagador.Substring(0, 50); };
                    BolecodePagadorPagadorEndereco = record_g_clientes.endereco_com.EmptyIfNull().ToString().Trim();
                    if (record_g_clientes.endereco_com_numero.EmptyIfNull().ToString().Length > 0) { BolecodePagadorPagadorEndereco += ", " + record_g_clientes.endereco_com_numero.EmptyIfNull().ToString().Trim(); };
                    if (record_g_clientes.endereco_com_complemento.EmptyIfNull().ToString().Length > 0) { BolecodePagadorPagadorEndereco += ", " + record_g_clientes.endereco_com_complemento.EmptyIfNull().ToString().Trim(); };
                    if (BolecodePagadorPagadorEndereco.Length > 45) { BolecodePagadorPagadorEndereco = BolecodePagadorPagadorEndereco.Substring(0, 45); };
                    BolecodePagadorPagadorBairro = record_g_clientes.bairro_com.EmptyIfNull().ToString().Trim();
                    if (BolecodePagadorPagadorBairro.Length > 15) { BolecodePagadorPagadorBairro = BolecodePagadorPagadorBairro.Substring(0, 15); };
                    BolecodePagadorPagadorCidade = db.g_cidades.Find(record_g_clientes.id_cidade_com).nome.EmptyIfNull().ToString();
                    if (BolecodePagadorPagadorCidade.Length > 20) { BolecodePagadorPagadorCidade = BolecodePagadorPagadorCidade.Substring(0, 20); };
                    BolecodePagadorPagadorCEP = LibStringFormat.SomenteNumeros(record_g_clientes.cep_com.EmptyIfNull().ToString().Trim());
                    BolecodePagadorPagadorUF = db.g_uf.Find(record_g_clientes.id_uf_com).nome.EmptyIfNull().ToString();

                    RoboItauBolecode roboItauBolecode = new RoboItauBolecode(RecordContaCaixaBancaria.id_conta_caixa, FileNameCertificadoItau);
                    ModelItauBolecode NewBolecode = new ModelItauBolecode();
                    NewBolecode.Bolecode_CodigoCanalOperacao = "API";
                    NewBolecode.Bolecode_CodigoOperador = "";
                    if (AmbienteProducao == true) { NewBolecode.Bolecode_EtapaProcessoBoleto = "efetivacao"; } else { NewBolecode.Bolecode_EtapaProcessoBoleto = "simulacao"; };
                    NewBolecode.Bolecode_Beneficiario_NomeCobranca = "GDI IMPORTACAO E COMERCIO DE PECAS AERONAUTICAS";
                    NewBolecode.Bolecode_Beneficiario_TipoPessoa_CodigoTipoPessoa = "J";

                    if (RecordContaCaixaBancaria.id_conta_caixa == 7)       // GDI BH
                    { 
                        NewBolecode.Bolecode_Beneficiario_IdBeneficiario = "561200998475";
                        NewBolecode.Bolecode_Beneficiario_TipoPessoa_NumeroCadastroNacionaPessoaJuridica = "561200998475";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NomeLogradouro = "IGNACINHO ALVARENGA, 35, LOJA  B";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NomeBairro = "VENDA NOVA";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NomeCidade = "BELO HORIZONTE";
                        NewBolecode.Bolecode_Beneficiario_Endereco_SiglaUF = "MG";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NumeroCep = "31610015";
                        NewBolecode.Bolecode_Beneficiario_Endereco_Numero = "35";
                        NewBolecode.Bolecode_Beneficiario_Endereco_Complemento = "LOJA  B";
                        NewBolecode.Bolecode_QrCode_Chave = "10623303000114";

                    }
                    else if (RecordContaCaixaBancaria.id_conta_caixa == 10) // GDI SP
                    { 
                        NewBolecode.Bolecode_Beneficiario_IdBeneficiario = "561200997626";
                        NewBolecode.Bolecode_Beneficiario_TipoPessoa_NumeroCadastroNacionaPessoaJuridica = "561200997626";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NomeLogradouro = "RUA EMB COELHO DE ALMEIDA, 71, SALA A";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NomeBairro = "PQ JABAQUARA";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NomeCidade = "SAO PAULO";
                        NewBolecode.Bolecode_Beneficiario_Endereco_SiglaUF = "SP";
                        NewBolecode.Bolecode_Beneficiario_Endereco_NumeroCep = "04355020";
                        NewBolecode.Bolecode_Beneficiario_Endereco_Numero = "71";
                        NewBolecode.Bolecode_Beneficiario_Endereco_Complemento = "SALA A";
                        NewBolecode.Bolecode_QrCode_Chave = "10623303000203";
                    }

                    NewBolecode.Bolecode_DadosBoleto_DescricaoInstrumentoCobranca = "boleto_pix";
                    NewBolecode.Bolecode_DadosBoleto_TipoBoleto = "a vista";
                    NewBolecode.Bolecode_DadosBoleto_CodigoCarteira = "109";
                    NewBolecode.Bolecode_DadosBoleto_ValorTotalTitulo = BolecodeValorTitulo.ToString("000000000000000.00").Replace(",", "").Replace(".", "");
                    NewBolecode.Bolecode_DadosBoleto_CodigoEspecie = "01";
                    NewBolecode.Bolecode_DadosBoleto_DataEmissao = BolecodeDataEmissao.ToString("yyyy-MM-dd");
                    NewBolecode.Bolecode_DadosBoleto_ValorAbatimento = "00000000000000000";
                    NewBolecode.Bolecode_DadosBoleto_CodigoTipoVencimento = "|3|";
                    NewBolecode.Bolecode_DadosBoleto_PagamentoParcial = "|false|";
                    NewBolecode.Bolecode_DadosBoleto_DescontoExpresso = "|false|";

                    NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_NomePessoa = BolecodeNomePagador;
                    NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_NomeFantasia = BolecodeNomePagador;
                    NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_CodigoTipoPessoa = BolecodeDocumentoTipo;
                    if (BolecodeDocumentoTipo == "F") { NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_NumeroCadastroPessoaFisica = BolecodePagadorDocumentoNumero; }
                    else { NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_NumeroCadastroNacionalPessoaJuridica = BolecodePagadorDocumentoNumero; }
                    NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NomeLogradouro = BolecodePagadorPagadorEndereco;
                    NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NomeBairro = BolecodePagadorPagadorBairro;
                    NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NomeCidade = BolecodePagadorPagadorCidade;
                    NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_siglaUF = BolecodePagadorPagadorUF;
                    NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NumeroCep = BolecodePagadorPagadorCEP;
                    NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_TextoSeuNumero = BolecodeNossoNumero.ToString("00000000");
                    NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_NumeroNossoNumero = BolecodeNossoNumero.ToString("00000000");
                    NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_DataVencimento = BolecodeDataVencimento.ToString("yyyy-MM-dd");
                    NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_TextoUsoBeneficiario = "000001";
                    NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_ValorTitulo = BolecodeValorTitulo.ToString("000000000000000.00").Replace(",", "").Replace(".", "");
                    NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_DataLimitePagamento = BolecodeDataLimitePagamento.ToString("yyyy-MM-dd");
                    NewBolecode.Bolecode_DadosBoleto_Multa_CodigoTipoMulta = "01"; // Valor Fixo Multa
                    NewBolecode.Bolecode_DadosBoleto_Multa_ValorMulta = BolecodeValorMulta.ToString("0000000000.00").Replace(",", "").Replace(".", "");
                    NewBolecode.Bolecode_DadosBoleto_Juros_DataJuros = BolecodeDataVencimento.AddDays(1).ToString("yyyy-MM-dd"); ; // Data dos Juros
                    NewBolecode.Bolecode_DadosBoleto_Juros_CodigoTipoJuros = "93"; // Valor Diário
                    NewBolecode.Bolecode_DadosBoleto_Juros_ValorJuros = BolecodeValorJuros.ToString("000000000000000.00").Replace(",", "").Replace(".", "");
                    if (NewBolecode.Bolecode_DadosBoleto_Juros_ValorJuros == "00000000000000000") { NewBolecode.Bolecode_DadosBoleto_Juros_ValorJuros = "00000000000000001"; };
                    ModelItauBolecode BolecodeProcessado = roboItauBolecode.RegistrarBolecode(NewBolecode, FileNameCertificadoItau);
                    if (NewBolecode.Registrado == true)
                    {
                        g_financeiro_gateway record_g_financeiro_gateway = db.g_financeiro_gateway.Find(IdFinanceiroGateway);
                        record_gc_financeiro_lancamentos.id_pag_rec_tipo = 3;
                        record_gc_financeiro_lancamentos.is_boleto = true;
                        record_gc_financeiro_lancamentos.boleto_banco = record_g_financeiro_gateway.codigo_banco;
                        record_gc_financeiro_lancamentos.boleto_datahora_geracao = DataHoraAtual;
                        record_gc_financeiro_lancamentos.boleto_id_usuario_geracao = CachePersister.userIdentity.IdUsuario; ;
                        record_gc_financeiro_lancamentos.cnab_linha_digitavel = NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_NumeroLinhaDigitavel.EmptyIfNull().ToString();
                        record_gc_financeiro_lancamentos.cnab_nosso_numero = record_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToString();
                        record_gc_financeiro_lancamentos.cnab_codigo_barras = NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_CodigoBarras.EmptyIfNull().Trim();
                        record_gc_financeiro_lancamentos.cnab_id_boleto_banco = NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_IdBoletoIndividual.EmptyIfNull().Trim();
                        record_gc_financeiro_lancamentos.pix_emv = NewBolecode.Bolecode_QrCode_Emv.EmptyIfNull().Trim();
                        record_gc_financeiro_lancamentos.pix_base64 = NewBolecode.Bolecode_QrCode_Base64.EmptyIfNull().Trim();
                        record_gc_financeiro_lancamentos.pix_txid = NewBolecode.Bolecode_QrCode_Txid.EmptyIfNull().Trim();
                        record_gc_financeiro_lancamentos.pix_location = NewBolecode.Bolecode_QrCode_Location.EmptyIfNull().Trim();
                        record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                        record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;
                        Resultado = record_gc_financeiro_lancamentos.id_lancamento;

                        // Criar o log da utilização
                        a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                        record_a_yesprodutos_extrato.id_yesproduto = 5; // Boleto Itaú
                        record_a_yesprodutos_extrato.log = "Boleto Itaú - Id: " + record_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToString();
                        record_a_yesprodutos_extrato.datahora_execucao = DataHoraAtual;
                        record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                        db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                        db.SaveChanges();
                    }
                    else
                    {
                        throw new Exception("Erro ao gerar o boleto Itaú on-line [" + NewBolecode.Bolecode_MsgErro.EmptyIfNull().ToString() + "]");
                    }
                    return Resultado;
                }
                else
                {
                    throw new Exception("Título financeiro não localizado!");
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
        #endregion

        #region ModalBaixarLancamentos
        public ActionResult ModalBaixarLancamentos(String id)
        {
            String MsgBloqueio = String.Empty;
            try
            {
                ViewBag.Title = "Baixar Lançamento Financeiro";
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(int.Parse(id));

                if (record_gc_financeiro_lancamentos.id_financeiro_status != 3)
                {
                    String StatusLancamento = db.gc_financeiro_status.Find(record_gc_financeiro_lancamentos.id_financeiro_status).nome.EmptyIfNull().ToString();
                    MsgBloqueio += " - Não é possível Baixar o lançamento, o mesmo se encontra no status ["+ StatusLancamento + "]<br/>";
                }
                record_gc_financeiro_lancamentos.motivo_baixa = String.Empty;
                ViewBag.MsgBloqueio = MsgBloqueio;
                return View(record_gc_financeiro_lancamentos);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroLancamentoController";
                msg += "<br/>" + "ModalBaixarLancamentos(" + id.ToString() + ")";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxModalBaixarLancamentos(gc_financeiro_lancamentos view_gc_financeiro_lancamentos)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(view_gc_financeiro_lancamentos.id_lancamento);
                if (record_gc_financeiro_lancamentos.id_financeiro_status != 3)
                {
                    qtdInconsistencias += 1;
                    String StatusLancamento = db.gc_financeiro_status.Find(record_gc_financeiro_lancamentos.id_financeiro_status).nome.EmptyIfNull().ToString();
                    MsgRetorno += " - Não é possível Baixar o lançamento, o mesmo se encontra no status [" + StatusLancamento + "]<br/>";
                }
                if (qtdInconsistencias == 0)
                {
                    record_gc_financeiro_lancamentos.ativo = false;
                    record_gc_financeiro_lancamentos.id_financeiro_status = 4; // Baixado
                    record_gc_financeiro_lancamentos.motivo_baixa = view_gc_financeiro_lancamentos.motivo_baixa;
                    record_gc_financeiro_lancamentos.datahora_baixa = DataHoraAtual;
                    record_gc_financeiro_lancamentos.id_usuario_baixa = CachePersister.userIdentity.IdUsuario;
                    record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                    record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;
                    db.SaveChanges();
                    Sucesso = true;
                    MsgRetorno += "Lançamento <b>Baixado</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");

                    if (record_gc_financeiro_lancamentos.id_movimento > 0)
                    {
                        String LogAlteracoes = "Baixa de Lançamento Financeiro | ";
                        LogAlteracoes += "Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString() + " - ";
                        LogAlteracoes += record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yy") + " - ";
                        LogAlteracoes += record_gc_financeiro_lancamentos.valor_total.ToString("N2") + " - ";
                        LogAlteracoes += db.g_pagrec_tipos.Where(t => t.id_pagrec_tipo == record_gc_financeiro_lancamentos.id_pag_rec_tipo).FirstOrDefault().descricao.EmptyIfNull().ToString() + " |";
                        LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_financeiro_lancamentos.id_movimento, LogAlteracoes);
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
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCancelarLancamentos
        public ActionResult ModalCancelarLancamentos(String id)
        {
            try
            {
                String MsgAdvertencia = String.Empty;
                String MsgBloqueio = String.Empty;
                ViewBag.Title = "Cancelar Lançamento Financeiro";
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(int.Parse(id));
                if (record_gc_financeiro_lancamentos.id_movimento > 0)
                {
                    MsgAdvertencia += " - Foi encontrado [Pedido/Cotação] relacionado à esse lançamento<br/>";
                }
                if (record_gc_financeiro_lancamentos.id_lancamento_tributos > 0)
                {
                    MsgAdvertencia += " - Foi encontrado [Provisão de Impostos] relacionada à esse lançamento<br/>";
                }
                if (record_gc_financeiro_lancamentos.id_movimento_nf > 0)
                {
                    MsgAdvertencia += " - Foi encontrado [Nota Fiscal] relacionada à esse lançamento<br/>";
                }
                if (record_gc_financeiro_lancamentos.id_financeiro_status != 3)
                {
                    String StatusLancamento = db.gc_financeiro_status.Find(record_gc_financeiro_lancamentos.id_financeiro_status).nome.EmptyIfNull().ToString();
                    MsgBloqueio += " - Não é possível Cancelar o lançamento, o mesmo se encontra no status [" + StatusLancamento + "]<br/>";
                }
                ViewBag.MsgAdvertencia = MsgAdvertencia;
                ViewBag.MsgBloqueio = MsgBloqueio;
                record_gc_financeiro_lancamentos.motivo_cancelamento = "";
                return View(record_gc_financeiro_lancamentos);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroLancamentoController";
                msg += "<br/>" + "ModalCancelarLancamentos(" + id.ToString() + ")";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxModalCancelarLancamentos(gc_financeiro_lancamentos view_gc_financeiro_lancamentos)
        {
            bool Sucesso = false;
            int QtdInconsistencias = 0;
            int QtdBoletosACancelar = 0;
            String MsgRetorno = String.Empty;
            String ListaIdsBoletosCancelar = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(view_gc_financeiro_lancamentos.id_lancamento);
                if (record_gc_financeiro_lancamentos.id_financeiro_status != 3)
                {
                    QtdInconsistencias += 1;
                    String StatusLancamento = db.gc_financeiro_status.Find(record_gc_financeiro_lancamentos.id_financeiro_status).nome.EmptyIfNull().ToString();
                    MsgRetorno += " - Não é possível Cancelar o lançamento, o mesmo se encontra no status [" + StatusLancamento + "]<br/>";
                }
                if (QtdInconsistencias == 0)
                {
                    record_gc_financeiro_lancamentos.ativo = false;
                    record_gc_financeiro_lancamentos.id_financeiro_status = 5; // Cancelado
                    record_gc_financeiro_lancamentos.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                    record_gc_financeiro_lancamentos.datahora_cancelamento = DataHoraAtual;
                    record_gc_financeiro_lancamentos.motivo_cancelamento = LibStringFormat.FormatarTextoCadastroNormal(view_gc_financeiro_lancamentos.motivo_cancelamento);
                    if (record_gc_financeiro_lancamentos.numero_documento != null) { record_gc_financeiro_lancamentos.numero_documento = LibStringFormat.FormatarTextoCadastroNormal(record_gc_financeiro_lancamentos.numero_documento); };
                    record_gc_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                    record_gc_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_financeiro_lancamentos).State = EntityState.Modified;


                    // CRIAR ATENDIMENTO
                    if (record_gc_financeiro_lancamentos.id_pag_rec_tipo == 3)
                    {
                        ListaIdsBoletosCancelar += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ",";
                        QtdBoletosACancelar += 1;

                        ListaIdsBoletosCancelar += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ",";
                        QtdBoletosACancelar += 1;
                        g_clientes record_g_clientes = db.g_clientes.Find(record_gc_financeiro_lancamentos.id_cliente);

                        g_atendimentos NovoAtendimento = new g_atendimentos();
                        NovoAtendimento.concluido = false;
                        NovoAtendimento.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                        NovoAtendimento.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                        NovoAtendimento.privado = false;
                        NovoAtendimento.enviar_atualizacoes = false;
                        NovoAtendimento.param_id_cliente = record_gc_financeiro_lancamentos.id_cliente;
                        NovoAtendimento.param_numero_pedido = record_gc_financeiro_lancamentos.id_movimento;
                        NovoAtendimento.param_numero_nf = 0;
                        NovoAtendimento.param_id_produto = -1;
                        NovoAtendimento.param_limite_credito = 0;
                        NovoAtendimento.param_id_vendedor = -1;
                        NovoAtendimento.id_status = 1; // Aberto
                        NovoAtendimento.id_atendimento_categoria = 1; // Pedidos - Faturamento / Boleto(Alterar / Cancelar)
                        NovoAtendimento.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                        NovoAtendimento.solicitacao_datahora = DataHoraAtual;
                        NovoAtendimento.responsavel_id_usuario = 0;
                        NovoAtendimento.responsavel_id_departamento = 1;
                        NovoAtendimento.id_coligada = 0;
                        NovoAtendimento.id_filial = 0;
                        NovoAtendimento.datahora_cadastro = DataHoraAtual;
                        NovoAtendimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        db.g_atendimentos.Add(NovoAtendimento);
                        db.SaveChanges();

                        g_atendimentos_atividades NovaAtividade = new g_atendimentos_atividades();
                        NovaAtividade.id_atendimento = NovoAtendimento.id_atendimento;
                        NovaAtividade.id_atendimento_categoria_atividade = 1;
                        NovaAtividade.concluido = false;
                        NovaAtividade.privado = false;
                        NovaAtividade.solicitacao = "Cancelar Boleto Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString();
                        NovaAtividade.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + record_gc_financeiro_lancamentos.numero_documento.EmptyIfNull().ToString() + " | Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                        NovaAtividade.ordem = 0;
                        NovaAtividade.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                        NovaAtividade.solicitacao_datahora = DataHoraAtual;
                        NovaAtividade.responsavel_id_usuario = 0;
                        NovaAtividade.responsavel_id_departamento = 1;
                        NovaAtividade.conclusao_id_usuario = 0;
                        NovaAtividade.cancelamento_id_usuario = 0;
                        NovaAtividade.id_coligada = 0;
                        NovaAtividade.id_filial = 0;
                        NovaAtividade.datahora_cadastro = DataHoraAtual;
                        NovaAtividade.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        db.g_atendimentos_atividades.Add(NovaAtividade);
                        db.SaveChanges();
                    }

                    db.SaveChanges();
                    Sucesso = true;
                    MsgRetorno += "Lançamento <b>Cancelado</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    if (QtdBoletosACancelar > 0) { MsgRetorno += "<br/>" + QtdBoletosACancelar.ToString() + " Boletos à Cancelar (" + ListaIdsBoletosCancelar.ToString() + ")" + "<br/>" + MsgGeral; };

                    if (record_gc_financeiro_lancamentos.id_movimento > 0)
                    {
                        String LogAlteracoes = "Cancelamento de Lançamento Financeiro | ";
                        LogAlteracoes += "Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString() + " - ";
                        LogAlteracoes += record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yy") + " - ";
                        LogAlteracoes += record_gc_financeiro_lancamentos.valor_total.ToString("N2") + " - ";
                        LogAlteracoes += db.g_pagrec_tipos.Where(t => t.id_pagrec_tipo == record_gc_financeiro_lancamentos.id_pag_rec_tipo).FirstOrDefault().descricao.EmptyIfNull().ToString() + " |";
                        LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_financeiro_lancamentos.id_movimento, LogAlteracoes);
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
            return Json(new { success = Sucesso, msg = MsgRetorno}, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region AjaxExportarLancamentosExcel
        [HttpPost]
        [GdiValidateAntiForgeryToken]
        public ActionResult AjaxExportarLancamentosExcel(String parametros)
        {
            bool Sucesso = false;
            bool CustomizacaoPauloSC = false;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String IdProcessamentoGravado = "0";
            String DirTemplate = String.Empty;
            String FileNameTemplate = String.Empty;
            String NomeContaCaixa = String.Empty;
            String SentencaSqlLancamentos = String.Empty;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_exportar_lancamentos_gdi.xls");

            try
            {
                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Lançamentos");

                List<Db.g_contas_caixas> ListaContasCaixas = db.g_contas_caixas.Where(c => c.ativo == true).ToList();
                g_contas_caixas record_g_contas_caixas = new Db.g_contas_caixas();
                record_g_contas_caixas.id_conta_caixa = 0;

                if (CachePersister.userIdentity.IdContaCaixaAtiva != "999") { record_g_contas_caixas = db.g_contas_caixas.Find(int.Parse(CachePersister.userIdentity.IdContaCaixaAtiva)); };

                List<String> ListaParametros = CachePersister.userIdentity.ParamSqlGcGetDadosLancamentos.Split(new string[] { ";" }, StringSplitOptions.None).ToList();
                String yesCustomField01 = ListaParametros[0];
                String yesCustomField02 = ListaParametros[1];
                String yesCustomField03 = ListaParametros[2];
                String yesCustomField04 = ListaParametros[3];
                String yesCustomField05 = ListaParametros[4];
                String yesCustomField06 = ListaParametros[5];
                String yesCustomField07 = ListaParametros[6];
                String yesCustomField08 = ListaParametros[7];
                String yesCustomField09 = ListaParametros[8];
                String yesCustomField10 = ListaParametros[9]; 
                String yesCustomField11 = ListaParametros[10];

                // =========================
                // 1) Parse filtros
                // =========================
                int idContaCaixa = 0;
                int.TryParse(yesCustomField01.EmptyIfNull().ToString().Trim(), out idContaCaixa);
                string filtroStatus = yesCustomField02.EmptyIfNull().ToString().Trim();
                string filtroDescricao = yesCustomField03.EmptyIfNull().ToString().Trim();
                string filtroIdLancamento = yesCustomField04.EmptyIfNull().ToString().Trim();
                string filtroNumeroDocumento = yesCustomField05.EmptyIfNull().ToString().Trim();
                string filtroValor = yesCustomField06.EmptyIfNull().ToString().Trim();
                string filtroCliFor = yesCustomField07.EmptyIfNull().ToString().Trim();
                string filtroHideGerencial = yesCustomField08.EmptyIfNull().ToString().Trim();
                DateTime data1 = DateTime.Now;
                DateTime data2 = DateTime.Now;
                DateTime dataAtual = DateTime.Now.Date;
                DateTime.TryParse(yesCustomField09.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out data1);
                DateTime.TryParse(yesCustomField10.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out data2);
                string filtroCustom01 = yesCustomField11.EmptyIfNull().ToString().Trim();
                if (string.IsNullOrWhiteSpace(filtroCustom01)) filtroCustom01 = "0";
                if ((filtroCliFor != "-1") || (filtroDescricao.Length > 0) || (filtroIdLancamento.Length > 0) || (filtroNumeroDocumento.Length > 0) || (filtroValor.Length > 0)) { try { data1 = data1.AddYears(-10); data2 = data2.AddYears(10); } catch { } };

                
                // =========================
                // 2) Controle de acesso Conta Caixa (sem try/catch gigante)
                // =========================
                CachePersister.userIdentity.param_contacaixa_gc_has_edit = false;
                CachePersister.userIdentity.param_contacaixa_gc_has_view = false;
                CachePersister.userIdentity.param_contacaixa_gc_has_gerencial = false;
                CachePersister.userIdentity.IdContaCaixaAtiva = yesCustomField01.EmptyIfNull().ToString().Trim();

                if (CachePersister.userIdentity.IdContaCaixaAtiva == "999")
                {
                    CachePersister.userIdentity.param_contacaixa_gc_has_edit = true;
                    CachePersister.userIdentity.param_contacaixa_gc_has_view = true;
                    CachePersister.userIdentity.param_contacaixa_gc_has_gerencial = true;
                }
                else
                {
                    var acesso = db.g_contas_caixas_acessos.AsNoTracking().FirstOrDefault(a => a.id_usuario == CachePersister.userIdentity.IdUsuario && a.id_conta_caixa == idContaCaixa);
                    if (acesso != null)
                    {
                        CachePersister.userIdentity.param_contacaixa_gc_has_edit = acesso.has_edit;
                        CachePersister.userIdentity.param_contacaixa_gc_has_view = acesso.has_view;
                        CachePersister.userIdentity.param_contacaixa_gc_has_gerencial = acesso.has_gerencial;
                    }
                }



                // =========================
                // 3) Query base EF (sem SQL concatenado)
                // =========================
                var lanc = db.gc_financeiro_lancamentos.AsNoTracking().Where(l => l.id_usuario_cadastro > 0 && l.ativo == true);
                if (idContaCaixa < 999)
                {
                    if (idContaCaixa == 4 || idContaCaixa == 13) lanc = lanc.Where(l => (l.id_conta_caixa == 4 || l.id_conta_caixa == 13));
                    else lanc = lanc.Where(l => l.id_conta_caixa == idContaCaixa);
                }
                lanc = lanc.Where(l => l.data_vencimento >= data1 && l.data_vencimento <= data2);

                // Status
                if (!string.IsNullOrWhiteSpace(filtroStatus)) { if (filtroStatus != "0") { if (int.TryParse(filtroStatus, out int idStatus)) lanc = lanc.Where(l => l.id_financeiro_status == idStatus); }; }

                // Gerencial
                if (!CachePersister.userIdentity.param_contacaixa_gc_has_gerencial) lanc = lanc.Where(l => l.gerencial == false); 
                    else if (!string.IsNullOrWhiteSpace(filtroHideGerencial)) lanc = lanc.Where(l => l.gerencial == false);

                // Filtros texto
                if (LibStringFormat.TryMontarPadraoLikeContemTexto(filtroDescricao, out string padraoDescricao2))
                {
                    lanc = lanc.Where(l => l.descricao != null && DbFunctions.Like(l.descricao, padraoDescricao2));
                }

                if (LibStringFormat.TryMontarPadraoLikeContemCodigo(filtroNumeroDocumento, out string padraoNumeroDoc2))
                {
                    lanc = lanc.Where(l => l.numero_documento != null && DbFunctions.Like(l.numero_documento, padraoNumeroDoc2));
                }

                if (!string.IsNullOrWhiteSpace(filtroIdLancamento))
                {
                    if (int.TryParse(filtroIdLancamento, out int idLan)) lanc = lanc.Where(l => l.id_lancamento == idLan);
                    else lanc = lanc.Where(l => false);
                }

                if (!string.IsNullOrWhiteSpace(filtroCliFor) && filtroCliFor != "0" && filtroCliFor != "-1")
                {
                    if (int.TryParse(filtroCliFor, out int idCli)) lanc = lanc.Where(l => l.id_cliente == idCli);
                    else lanc = lanc.Where(l => false);
                }

                if (!string.IsNullOrWhiteSpace(filtroValor))
                {
                    if (decimal.TryParse(filtroValor, NumberStyles.Any, new CultureInfo("pt-BR"), out decimal v) || decimal.TryParse(filtroValor, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    {
                        lanc = lanc.Where(l => l.valor_total == v || l.valor_pago == v);
                    }
                }

                // Custom categoria
                if (filtroCustom01 == "1") lanc = lanc.Where(l => l.is_adiantamento == true);
                else if (filtroCustom01 == "2") lanc = lanc.Where(l => l.is_provisao_imposto == true);
                else if (filtroCustom01 == "3") lanc = lanc.Where(l => l.is_difal == true);

                // =========================
                // 4) Ordenação (OrderBy antes do Skip/Take)
                // =========================
                var ordered = lanc
                    .OrderByDescending(l => l.data_pagamento)
                    .ThenByDescending(l => l.ordem_pagamento)
                    .ThenByDescending(l => l.id_lancamento);

                var listaExportExcel = ordered.ToList();

                var allRecordsClientes = db.g_clientes.Select(g => new { g.id_cliente, g.nome }).ToList();

                if (listaExportExcel.Count > 0)
                {
                    if (CustomizacaoPauloSC == true) { NomeContaCaixa = "SC VENDAS - BH e SP"; }
                    else if (CachePersister.userIdentity.IdContaCaixaAtiva != "999") { NomeContaCaixa = record_g_contas_caixas.nome.EmptyIfNull().ToString(); } 
                    else { NomeContaCaixa = "Todas"; };
                    
                    sheetCatalogo.GetCell(2, 1).SetCellValue("Conta Caixa: " + NomeContaCaixa);
                    sheetCatalogo.GetCell(1, 8).SetCellValue(CachePersister.userIdentity.SaldoContaCaixaAtiva);

                    foreach (var l in listaExportExcel)
                    {
                        NomeContaCaixa = ListaContasCaixas.Where(c => c.id_conta_caixa == l.id_conta_caixa).FirstOrDefault().nome.EmptyIfNull().ToString();

                        IndexLinha += 1;
                        string NomeCliente = string.Empty;
                        if (l.id_cliente > 0)
                        {
                            try { NomeCliente = allRecordsClientes.Find(c => c.id_cliente == l.id_cliente).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                        }
                        if (l.descricao.EmptyIfNull().ToString().Length > 0)
                        {
                            if (NomeCliente.EmptyIfNull().ToString().Length > 0) { NomeCliente += " - "; };
                            NomeCliente += l.descricao.EmptyIfNull().ToString().Trim();
                        }
                        if (l.fixo == false)
                        {
                            NomeCliente += " (" + l.parcela_atual.EmptyIfNull().ToString() + "/" + l.parcela_total.EmptyIfNull().ToString() + ")";
                        }
                        string DescStatus = String.Empty;
                        if (l.id_financeiro_status == 1) { DescStatus = "Liquidado"; }
                        else if (l.id_financeiro_status == 2) { DescStatus = "Baixa Parcial"; }
                        else if (l.id_financeiro_status == 3) { DescStatus = "Aberto"; }
                        else if (l.id_financeiro_status == 4) { DescStatus = "Cancelado"; }

                        string DescTipoPagRec = String.Empty;

                        if (l.gerencial == true)
                        {
                            if (l.tipo_pag_rec == 1) { DescTipoPagRec = "Débito *"; }
                            else if (l.tipo_pag_rec == 2) { DescTipoPagRec = "Crédito *"; }
                        }
                        else
                        {
                            if (l.tipo_pag_rec == 1) { DescTipoPagRec = "Débito"; }
                            else if (l.tipo_pag_rec == 2) { DescTipoPagRec = "Crédito"; }
                        }

                        string ValorTotal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                        string ValorPago = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.valor_pago).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                        if (l.tipo_pag_rec == 1)
                        {
                            if (l.valor_total > 0) { ValorTotal = "-" + ValorTotal; };
                            if (l.valor_pago > 0) { ValorPago = "-" + ValorPago; };
                        }
                        String DataPagamento = l.data_pagamento.ToString("dd/MM/yy").Replace("01/01/01", "").Replace("01/01/99", "");
                        sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(l.id_lancamento);
                        sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(NomeContaCaixa);
                        sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(DescTipoPagRec);
                        sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue(DescStatus);
                        if (DataPagamento.Trim().Length > 0) { sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(DateTime.Parse(DataPagamento)); }
                        else { sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(DataPagamento); }
                        sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(DateTime.Parse(l.data_vencimento.ToString("dd/MM/yy")));
                        sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(l.numero_documento.EmptyIfNull().ToString());
                        sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(NomeCliente);
                        sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(Double.Parse(ValorTotal));
                        sheetCatalogo.GetCell(IndexLinha, 10).SetCellValue(Double.Parse(ValorPago));
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    String FileNameExportacao = Path.Combine(DirTempFiles, "Relatório-Lançamentos-Financeiros-" + LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + ".xls");
                    FileStream fileStream = new FileStream(FileNameExportacao, FileMode.Create);

                    using (FileStream FileSaida = fileStream)
                    {
                        _workbookCatalogo.Write(FileSaida);
                        FileSaida.Close();
                        FileTemplate.Close();
                    }

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                    record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                    record_g_processamento.detalhamento = "Relatório Lançamentos Financeiros";
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
                    MsgRetorno = "Arquivo GERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>" +
                                 "Qtd. Lançamentos Exportados.: " + record_g_processamento.qtd_reg_ok.ToString();
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
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
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Financeiro Movimento
        public ActionResult ModalGerarBoletoLancamentoAvulso(int? id)
        {
            int IdLancamento = id.GetValueOrDefault();
            gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(IdLancamento);
            if (record_gc_financeiro_lancamentos == null || IdLancamento <= 0)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Lançamento financeiro", id);
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-credit-card", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Gerar Boleto (não localizado)";
                return View(new gc_financeiro_lancamentos { id_lancamento = 0 });
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-credit-card", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Gerar Boleto - Lançamento Nº " + IdLancamento.ToString();
            return View(record_gc_financeiro_lancamentos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxModalGerarBoletoLancamentoAvulso(gc_financeiro_lancamentos view_gc_financeiro_lancamentos)
        {
            int qtdInconsistencias = 0;
            int IdFinanceiroGateway = 1; // 1 - Bradesco | 2 - Itaú
            bool sucesso = false;
            bool AmbienteProducao = true;
            String msgRetorno = "";
            gc_parametros record_gc_parametros = db.gc_parametros.Find(1);
            g_financeiro_gateway record_g_financeiro_gateway = new Db.g_financeiro_gateway();
            if (record_gc_parametros != null)
            {
                if (record_gc_parametros.id_financeiro_gateway > 0) { IdFinanceiroGateway = record_gc_parametros.id_financeiro_gateway; };
                record_g_financeiro_gateway = db.g_financeiro_gateway.Find(IdFinanceiroGateway);
                if (record_g_financeiro_gateway != null) { AmbienteProducao = record_g_financeiro_gateway.producao; }
            }
            gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(view_gc_financeiro_lancamentos.id_lancamento);
            try
            {
                if (record_gc_financeiro_lancamentos == null)
                {
                    msgRetorno += " - Lançamento financeiro não encontrado" + "<br/>";
                    qtdInconsistencias += 1;
                }
                else
                {
                    if (record_gc_financeiro_lancamentos.ativo == false)
                    {
                        msgRetorno += " - Lançamento financeiro não está ativo" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                    if (record_gc_financeiro_lancamentos.tipo_pag_rec == 1)
                    {
                        msgRetorno += " - Lançamento financeiro é de pagamento" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                    if (record_gc_financeiro_lancamentos.is_provisao_imposto == true)
                    {
                        msgRetorno += " - Lançamento financeiro é de provisão de imposto" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                    if (record_gc_financeiro_lancamentos.is_difal == true)
                    {
                        msgRetorno += " - Lançamento financeiro é de difal" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                    if (record_gc_financeiro_lancamentos.id_financeiro_status != 3)
                    {
                        msgRetorno += " - Lançamento financeiro não está aberto" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                    if (record_gc_financeiro_lancamentos.id_cliente == 0)
                    {
                        msgRetorno += " - Lançamento financeiro não tem cliente associado" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                    if (record_gc_financeiro_lancamentos.cnab_nosso_numero.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        msgRetorno += " - Lançamento financeiro já possui boleto associado" + "<br/>";
                        qtdInconsistencias += 1;
                    }

                    // Validação da geração de boletos pela conta caixa
                    g_contas_caixas RecordContaCaixaLancamento = db.g_contas_caixas.Find(record_gc_financeiro_lancamentos.id_conta_caixa);
                    if (RecordContaCaixaLancamento != null)
                    {
                        if (RecordContaCaixaLancamento.id_conta_bancaria > 0)
                        {
                            g_contas_caixas RecordContaCaixaBancaria = db.g_contas_caixas.Find(RecordContaCaixaLancamento.id_conta_bancaria);

                            if (RecordContaCaixaBancaria != null)
                            {
                                if (RecordContaCaixaBancaria.boleto_emissao == false)
                                {
                                    msgRetorno += " - Conta Caixa Bancária não permite a geração de Boletos!" + "<br/>";
                                    qtdInconsistencias += 1;
                                }
                            }
                            else
                            {
                                msgRetorno += " - Conta Caixa Bancária não localizada!" + "<br/>";
                                qtdInconsistencias += 1;
                            }
                        }
                        else
                        {
                            msgRetorno += " - Conta Caixa não permite a geração de Boletos!" + "<br/>";
                            qtdInconsistencias += 1;
                        }
                    }
                    else
                    {
                        msgRetorno += " - Conta Caixa não localizada!" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                }
                if (qtdInconsistencias == 0)
                {
                    MsgGeral = "Parcela " + record_gc_financeiro_lancamentos.parcela_atual.EmptyIfNull().ToString() + "  |  Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString() + "  |  Venc: " + record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yyyy") + "  |  R$: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_financeiro_lancamentos.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                    int RetornoBoleto = 0;
                    RetornoBoleto = RegistrarBolecodeItau(record_gc_financeiro_lancamentos.id_lancamento, IdFinanceiroGateway, AmbienteProducao);  // Itaú
                    if (RetornoBoleto > 0)
                    {
                        MsgGeral += "  |  Boleto On-Line gerado!<br/>";
                        msgRetorno = "<b>Boleto Gerado com Sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                        sucesso = true;

                        if (record_gc_financeiro_lancamentos.id_movimento > 0)
                        {
                            String LogAlteracoes = "Geração de Boleto Avulso | ";
                            LogAlteracoes += "Id: " + record_gc_financeiro_lancamentos.id_lancamento.ToString() + " - ";
                            LogAlteracoes += record_gc_financeiro_lancamentos.data_vencimento.ToString("dd/MM/yy") + " - ";
                            LogAlteracoes += record_gc_financeiro_lancamentos.valor_total.ToString("N2") + " - ";
                            LogAlteracoes += db.g_pagrec_tipos.Where(t => t.id_pagrec_tipo == record_gc_financeiro_lancamentos.id_pag_rec_tipo).FirstOrDefault().descricao.EmptyIfNull().ToString() + " | ";
                            LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_financeiro_lancamentos.id_movimento, LogAlteracoes);
                        }
                    }
                    else
                    {
                        msgRetorno = "<b>Erro na Geração do Boleto!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-down", "", "cc0000", "");
                        sucesso = false;
                    }
                }
                msgRetorno += "<br/><br/>" + MsgGeral;
                db.SaveChanges();
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

        #region Cancelar Movimento Financeiro
        public ActionResult ModalCancelarMovimentoFinanceiro(String id)
        {
            String MsgBloqueio = string.Empty;
            String MsgAdvertencia = String.Empty;
            try
            {
                int IdMovimento = 0;
                int.TryParse(id, out IdMovimento);
                int QtdTitulosFinanceirosLiquidados = 0;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_1 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_2 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_3 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_4 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_5 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_6 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_7 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_8 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_9 = null;
                gc_financeiro_lancamentos record_g_financeiro_lancamentos_10 = null;

                ViewBag.Title = "Cancelar Faturamento";
                gc_movimentos_financeiros record_gc_movimentos_financeiros = db.gc_movimentos_financeiros.Where(f => f.id_movimento == IdMovimento && f.ativo == true).FirstOrDefault();
                if (record_gc_movimentos_financeiros != null)
                {
                    record_gc_movimentos_financeiros.flag = true;
                    if ((record_gc_movimentos_financeiros.ativo == false) || (record_gc_movimentos_financeiros.movimento_faturado == false))
                    {
                        record_gc_movimentos_financeiros.flag = false;
                        MsgBloqueio += " - Movimento financeiro não localizado!" + "</br>";
                    }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_1 > 0) { record_g_financeiro_lancamentos_1 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_1); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_2 > 0) { record_g_financeiro_lancamentos_2 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_2); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_3 > 0) { record_g_financeiro_lancamentos_3 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_3); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_4 > 0) { record_g_financeiro_lancamentos_4 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_4); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_5 > 0) { record_g_financeiro_lancamentos_5 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_5); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_6 > 0) { record_g_financeiro_lancamentos_6 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_6); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_7 > 0) { record_g_financeiro_lancamentos_7 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_7); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_8 > 0) { record_g_financeiro_lancamentos_8 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_8); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_9 > 0) { record_g_financeiro_lancamentos_9 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_9); }
                    if (record_gc_movimentos_financeiros.id_financeiro_lancamento_10 > 0) { record_g_financeiro_lancamentos_10 = db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_10); }

                    if ((record_g_financeiro_lancamentos_1 != null) && (record_g_financeiro_lancamentos_1.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_2 != null) && (record_g_financeiro_lancamentos_2.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_3 != null) && (record_g_financeiro_lancamentos_3.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_4 != null) && (record_g_financeiro_lancamentos_4.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_5 != null) && (record_g_financeiro_lancamentos_5.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_6 != null) && (record_g_financeiro_lancamentos_6.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_7 != null) && (record_g_financeiro_lancamentos_7.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_8 != null) && (record_g_financeiro_lancamentos_8.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_9 != null) && (record_g_financeiro_lancamentos_9.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };
                    if ((record_g_financeiro_lancamentos_10 != null) && (record_g_financeiro_lancamentos_10.id_financeiro_status == 1)) { QtdTitulosFinanceirosLiquidados += 1; };

                    if (QtdTitulosFinanceirosLiquidados > 0)
                    {
                        record_gc_movimentos_financeiros.flag = false;
                        MsgBloqueio += " - Foram localizados " + QtdTitulosFinanceirosLiquidados.ToString() + " Títulos financeiros liquidados nesse movimento!" + "</br>";
                    }

                    String SqlNotasAutorizadas = "select nf.* from gc_movimentos_nf nf where nf.id_movimento = " + IdMovimento.ToString() + " and nf.id_nfe_status in (select distinct id_nfe_status from g_nfe_status where nf_ativa = 1)";
                    List<Db.gc_movimentos_nf> ListaNotasFiscaisAutorizadas = db.gc_movimentos_nf.SqlQuery(SqlNotasAutorizadas).ToList();
                    if (ListaNotasFiscaisAutorizadas.Count > 0)
                    {
                        if (ListaNotasFiscaisAutorizadas.Count == 1) { MsgAdvertencia = "<b>ATENÇÃO: Existe 1 Nota Fiscal Eletrônica (Ativa) associada à esse Pedido<br/>Verifique novamente o procedimento operacional antes do cancelamento financeiro.<b>"; }
                        else { MsgAdvertencia = "<b>ATENÇÃO: Existem " + ListaNotasFiscaisAutorizadas.Count().ToString() + " Notas Fiscais Eletrônica (Ativa) associadas à esse Pedido<br/>Verifique novamente o procedimento operacional antes do cancelamento financeiro.<b>"; }
                    }
                }
                else
                {
                    MsgBloqueio += " - Movimento financeiro não localizado!" + "</br>";
                    record_gc_movimentos_financeiros = new Db.gc_movimentos_financeiros();
                    record_gc_movimentos_financeiros.flag = false;
                }
                ViewBag.MsgBloqueio = MsgBloqueio;
                ViewBag.MsgAdvertencia = MsgAdvertencia;
                return View(record_gc_movimentos_financeiros);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroLancamentoController";
                msg += "<br/>" + "ModalCancelarMovimentoFinanceiro(" + id.ToString() + ")";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxModalCancelarMovimentoFinanceiro(gc_movimentos_financeiros view_gc_movimentos_financeiros)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            String StrTemp =  String.Empty;
            String LogAlteracoes = "Cancelamento movimento financeiro | ";
            int QtdTitulosFinanceirosCancelados = 0;
            int QtdBoletosACancelar = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            List<gc_financeiro_lancamentos> ListaLancamentosCancelar = new List<gc_financeiro_lancamentos>();
            List<g_pagrec_tipos> ListaPagRecTipos = db.g_pagrec_tipos.Where(t => t.id_pagrec_tipo > 0).ToList();

            try
            {
                gc_movimentos_financeiros record_gc_movimentos_financeiros = db.gc_movimentos_financeiros.Find(view_gc_movimentos_financeiros.id_movimento_financeiro);
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_gc_movimentos_financeiros.id_movimento);
                g_clientes record_g_clientes = db.g_clientes.Find(record_gc_movimento.id_cliente);
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_1 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_1)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_2 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_2)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_3 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_3)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_4 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_4)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_5 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_5)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_6 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_6)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_7 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_7)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_8 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_8)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_9 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_9)); }
                if (record_gc_movimentos_financeiros.id_financeiro_lancamento_10 > 0) { ListaLancamentosCancelar.Add(db.gc_financeiro_lancamentos.Find(record_gc_movimentos_financeiros.id_financeiro_lancamento_10)); }
                
                foreach (gc_financeiro_lancamentos RecordLancamentoCancelar in ListaLancamentosCancelar)
                {
                    if (RecordLancamentoCancelar != null)
                    {
                        LogAlteracoes += "Id: " + RecordLancamentoCancelar.id_lancamento.ToString() + ", ";
                        LogAlteracoes += RecordLancamentoCancelar.data_vencimento.ToString("dd/MM/yy") + " - ";
                        LogAlteracoes += RecordLancamentoCancelar.valor_total.ToString("N2") + " - ";
                        LogAlteracoes += ListaPagRecTipos.Find(t => t.id_pagrec_tipo == RecordLancamentoCancelar.id_pag_rec_tipo).descricao.EmptyIfNull().ToString() + " | ";

                        RecordLancamentoCancelar.ativo = false;
                        RecordLancamentoCancelar.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                        RecordLancamentoCancelar.datahora_cancelamento = DataHoraAtual;
                        RecordLancamentoCancelar.motivo_cancelamento = view_gc_movimentos_financeiros.motivo_cancelamento;
                        RecordLancamentoCancelar.datahora_alteracao = DataHoraAtual;
                        RecordLancamentoCancelar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordLancamentoCancelar).State = EntityState.Modified;
                        QtdTitulosFinanceirosCancelados += 1;

                        // CRIAR ATENDIMENTO
                        if (RecordLancamentoCancelar.id_pag_rec_tipo == 3)
                        {
                            QtdBoletosACancelar += 1;

                            g_atendimentos NovoAtendimento = new g_atendimentos();
                            NovoAtendimento.concluido = false;
                            NovoAtendimento.solicitacao = "Cancelar Boleto Id: " + RecordLancamentoCancelar.id_lancamento.ToString();
                            NovoAtendimento.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + RecordLancamentoCancelar.numero_documento.EmptyIfNull().ToString() + " | Venc: " + RecordLancamentoCancelar.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordLancamentoCancelar.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                            NovoAtendimento.privado = false;
                            NovoAtendimento.enviar_atualizacoes = false;
                            NovoAtendimento.param_id_cliente = RecordLancamentoCancelar.id_cliente;
                            NovoAtendimento.param_numero_pedido = RecordLancamentoCancelar.id_movimento;
                            NovoAtendimento.param_numero_nf = 0;
                            NovoAtendimento.param_id_produto = -1;
                            NovoAtendimento.param_limite_credito = 0;
                            NovoAtendimento.param_id_vendedor = -1;
                            NovoAtendimento.id_status = 1; // Aberto
                            NovoAtendimento.id_atendimento_categoria = 1; // Pedidos - Faturamento / Boleto(Alterar / Cancelar)
                            NovoAtendimento.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                            NovoAtendimento.solicitacao_datahora = DataHoraAtual;
                            NovoAtendimento.responsavel_id_usuario = 0;
                            NovoAtendimento.responsavel_id_departamento = 1;
                            NovoAtendimento.id_coligada = 0;
                            NovoAtendimento.id_filial = 0;
                            NovoAtendimento.datahora_cadastro = DataHoraAtual;
                            NovoAtendimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            db.g_atendimentos.Add(NovoAtendimento);
                            db.SaveChanges();

                            g_atendimentos_atividades NovaAtividade = new g_atendimentos_atividades();
                            NovaAtividade.id_atendimento = NovoAtendimento.id_atendimento;
                            NovaAtividade.id_atendimento_categoria_atividade = 1;
                            NovaAtividade.concluido = false;
                            NovaAtividade.privado = false;
                            NovaAtividade.solicitacao = "Cancelar Boleto Id: " + RecordLancamentoCancelar.id_lancamento.ToString();
                            NovaAtividade.descricao = "Cancelar Boleto | Cliente: " + record_g_clientes.nome_fantasia.EmptyIfNull().ToString() + " | Doc: " + RecordLancamentoCancelar.numero_documento.EmptyIfNull().ToString() + " | Venc: " + RecordLancamentoCancelar.data_vencimento.ToString("dd/MM/yyyy") + " | Valor: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordLancamentoCancelar.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                            NovaAtividade.ordem = 0;
                            NovaAtividade.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                            NovaAtividade.solicitacao_datahora = DataHoraAtual;
                            NovaAtividade.responsavel_id_usuario = 0;
                            NovaAtividade.responsavel_id_departamento = 1;
                            NovaAtividade.conclusao_id_usuario = 0;
                            NovaAtividade.cancelamento_id_usuario = 0;
                            NovaAtividade.id_coligada = 0;
                            NovaAtividade.id_filial = 0;
                            NovaAtividade.datahora_cadastro = DataHoraAtual;
                            NovaAtividade.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            db.g_atendimentos_atividades.Add(NovaAtividade);
                            db.SaveChanges();
                        }
                        db.SaveChanges();
                    }
                }

                // Atualização do movimento
                record_gc_movimento.movimento_faturado = false;
                if (record_gc_movimento.id_movimento_posicao == 3) { record_gc_movimento.id_movimento_posicao = 2; }; // De Faturado para Separado
                record_gc_movimento.datahora_alteracao = DataHoraAtual;
                record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_gc_movimento).State = EntityState.Modified;

                // Desativação do movimento financeiro
                record_gc_movimentos_financeiros.ativo = false;
                record_gc_movimentos_financeiros.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                record_gc_movimentos_financeiros.datahora_cancelamento = DataHoraAtual;
                record_gc_movimentos_financeiros.motivo_cancelamento = view_gc_movimentos_financeiros.motivo_cancelamento;

                db.SaveChanges();
                msgRetorno += "Faturamento <b>"+ view_gc_movimentos_financeiros.id_movimento.EmptyIfNull().ToString() + "</b> CANCELADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                if (QtdTitulosFinanceirosCancelados > 0) { msgRetorno += "</br></br>" + QtdTitulosFinanceirosCancelados.EmptyIfNull().ToString() + " Lançamento(s) financeiros cancelados!"; }
                if (QtdBoletosACancelar > 0) 
                { 
                    msgRetorno += "</br></br>" + QtdBoletosACancelar.EmptyIfNull().ToString() + " Boleto(s) a serem cancelados!";
                    LogAlteracoes += QtdBoletosACancelar.EmptyIfNull().ToString() + " Boleto(s) a serem cancelados | ";
                }
                sucesso = true;

                if (sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true,"gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); }; };
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

        #region ModalRelatorioContaCaixaSaldoDiario
        public ActionResult ModalRelatorioContaCaixaSaldoDiario()
        {
            try
            {
                DateTime UltimoDiaCompetencia = LibDateTime.getDataHoraBrasilia();
                DateTime PrimeiroDiaCompetencia = UltimoDiaCompetencia.AddMonths(-1);

                ViewBag.Title = LibIcons.getIcon("fa-regular fa-file-excel", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Relatório - Saldo Contas Caixas</b>";
                ViewBag.Msg = "Período: " + PrimeiroDiaCompetencia.ToString("dd/MM/yyyy") + " à " + UltimoDiaCompetencia.ToString("dd/MM/yyyy");
                return View();
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroLancamentoController";
                msg += "<br/>" + "ModalRelatorioContaCaixaSaldoDiario";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxModalRelatorioContaCaixaSaldoDiario(g_contas_caixas view_g_contas_caixas)
        {
            bool Sucesso = false;
            int IndexLinhaCC = 0;
            int IndexColunaCC = 0;
            String MsgRetorno = String.Empty;
            String StrTemp = String.Empty;
            String SaldoContaCaixaAtual = String.Empty;
            String SaldoContaCaixaDia = String.Empty;
            String IdProcessamentoGravado = "0";
            List<CstRowExtratoContaCaixa> ListaExtratoContaCaixa = new List<CstRowExtratoContaCaixa>();
            List<CstRowExtratoContaCaixa> ListaExtratoConsolidadoContaCaixa = new List<CstRowExtratoContaCaixa>();
            Decimal SaldoFinalContaCaixa = 0;
            Decimal SaldoFinalConsolidado = 0;
            Decimal SaldoDiarioContaCaixa = 0;
            DateTime UltimoDiaCompetencia = LibDateTime.getDataHoraBrasilia();
            DateTime PrimeiroDiaCompetencia = UltimoDiaCompetencia.AddMonths(-1);
            String ArquivoSaida = String.Empty;
            String FileNameExportacao = String.Empty;
            //String IdProcessamentoGravado = "0";
            String DirTemplate = String.Empty;
            String FileNameTemplate = String.Empty;
            //int IndexLinha = 0;
            FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_financeiro_extrato_cc.xls");

            try
            {
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Extrato");

                List<Db.g_contas_caixas> ListaContasCaixas = db.g_contas_caixas.Where(c => c.ativo == true && c.is_gerencial == true).OrderBy(c => c.ordem).ToList();
                String SqlSaldosContasCaixas = String.Empty;
                SqlSaldosContasCaixas += " select l.id_conta_caixa, ";
                SqlSaldosContasCaixas += "     SUM(CASE WHEN tipo_pag_rec = 1 THEN valor_pago ELSE 0 END) total_pago, ";
                SqlSaldosContasCaixas += "     SUM(CASE WHEN tipo_pag_rec = 2 THEN valor_pago ELSE 0 END) total_recebido ";
                SqlSaldosContasCaixas += " from gc_financeiro_lancamentos l ";
                SqlSaldosContasCaixas += " where (l.id_usuario_cadastro > 0) and (l.ativo = 1) and (id_financeiro_status != 3) and (id_financeiro_status != 5) and (l.data_pagamento >= '2022-06-01 00:00:00') ";
                SqlSaldosContasCaixas += " group by id_conta_caixa ";
                System.Data.DataTable TableSaldoContaCaixa = LibDB.GetDataTable(SqlSaldosContasCaixas, db);

                String SqlExtratoContasCaixas = String.Empty;
                SqlExtratoContasCaixas += " select l.id_conta_caixa, l.data_pagamento, ";
                SqlExtratoContasCaixas += "     SUM(CASE WHEN tipo_pag_rec = 1 THEN valor_pago ELSE 0 END) total_pago, ";
                SqlExtratoContasCaixas += "     SUM(CASE WHEN tipo_pag_rec = 2 THEN valor_pago ELSE 0 END) total_recebido ";
                SqlExtratoContasCaixas += " from gc_financeiro_lancamentos l ";
                SqlExtratoContasCaixas += " where (l.id_usuario_cadastro > 0) and (l.ativo = 1) and (id_financeiro_status != 3) and (id_financeiro_status != 5) and (l.data_pagamento >= '2022-06-01 00:00:00') ";
                SqlExtratoContasCaixas += " and (l.data_pagamento between '" + PrimeiroDiaCompetencia.ToString("yyyy-MM-dd") + " 00:00:00" + "' and '" + UltimoDiaCompetencia.ToString("yyyy-MM-dd") + " 23:59:59') ";
                SqlExtratoContasCaixas += " group by l.id_conta_caixa, l.data_pagamento ";
                SqlExtratoContasCaixas += " order by l.id_conta_caixa, l.data_pagamento desc ";
                System.Data.DataTable TableExtratoContaCaixa = LibDB.GetDataTable(SqlExtratoContasCaixas, db);

                if (TableExtratoContaCaixa.Rows.Count > 0)
                {
                    foreach (DataRow RowExtrato in TableExtratoContaCaixa.AsEnumerable())
                    {
                        CstRowExtratoContaCaixa RowExtratoContaCaixa = new CstRowExtratoContaCaixa();
                        RowExtratoContaCaixa.id_conta_caixa = Convert.ToInt32(RowExtrato["id_conta_caixa"]);
                        RowExtratoContaCaixa.data_pagamento = Convert.ToDateTime(RowExtrato["data_pagamento"]);
                        RowExtratoContaCaixa.total_pago = Convert.ToDecimal(RowExtrato["total_pago"]);
                        RowExtratoContaCaixa.total_recebido = Convert.ToDecimal(RowExtrato["total_recebido"]);
                        try { RowExtratoContaCaixa.saldo_dia = RowExtratoContaCaixa.total_recebido - RowExtratoContaCaixa.total_pago; } catch (Exception) { RowExtratoContaCaixa.saldo_dia = 0; };
                        ListaExtratoContaCaixa.Add(RowExtratoContaCaixa);
                    }
                }

                foreach (g_contas_caixas record_conta_caixa in ListaContasCaixas)
                {
                    IndexLinhaCC = 5;
                    SaldoFinalContaCaixa = 0;
                    SaldoDiarioContaCaixa = 0;
                    var RowSaldoContaCaixa = TableSaldoContaCaixa.AsEnumerable().Where(s => s.Field<int>("id_conta_caixa") == record_conta_caixa.id_conta_caixa).FirstOrDefault();
                    if (RowSaldoContaCaixa != null)
                    {
                        Decimal TotalPago = Decimal.Parse(RowSaldoContaCaixa["total_pago"].EmptyIfNull().ToString().Trim());
                        Decimal TotalRecebido = Decimal.Parse(RowSaldoContaCaixa["total_recebido"].EmptyIfNull().ToString().Trim());
                        SaldoFinalContaCaixa = TotalRecebido - TotalPago;
                        SaldoDiarioContaCaixa = SaldoFinalContaCaixa;
                    }
                    sheetCatalogo.GetCell(3, IndexColunaCC + 1).SetCellValue(record_conta_caixa.nome.EmptyIfNull().ToString() + "   (" + record_conta_caixa.tag_saldo_dia.EmptyIfNull().ToString() + ")");
                    DateTime DataExtrato = new DateTime(UltimoDiaCompetencia.Year, UltimoDiaCompetencia.Month, UltimoDiaCompetencia.Day);
                    DateTime DataInicioExtrato = DataExtrato.AddMonths(-1);
                    while (DataExtrato >= DataInicioExtrato)
                    {
                        sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 1).SetCellValue(DataExtrato.ToString("dd/MM/yyyy"));
                        CstRowExtratoContaCaixa RowExtratoContaCaixa = ListaExtratoContaCaixa.Where(c => c.id_conta_caixa == record_conta_caixa.id_conta_caixa && c.data_pagamento == DataExtrato).FirstOrDefault();
                        if (IndexLinhaCC == 5)
                        {
                            sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 5).SetCellValue(Decimal.ToDouble(SaldoFinalContaCaixa));
                        }
                        if (RowExtratoContaCaixa != null)
                        {
                            SaldoDiarioContaCaixa = SaldoDiarioContaCaixa - RowExtratoContaCaixa.saldo_dia;
                            sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 2).SetCellValue(Decimal.ToDouble(RowExtratoContaCaixa.total_recebido));
                            sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 3).SetCellValue(Decimal.ToDouble(RowExtratoContaCaixa.total_pago));
                            sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 4).SetCellValue(Decimal.ToDouble(RowExtratoContaCaixa.saldo_dia));
                            sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC+ 5).SetCellValue(Decimal.ToDouble(SaldoDiarioContaCaixa));
                        }
                        IndexLinhaCC += 1;
                        DataExtrato = DataExtrato.AddDays(-1);
                    }
                    IndexColunaCC += 6;
                }


                // Saldo Consolidado
                IndexLinhaCC = 5;
                SaldoFinalConsolidado = 0;
                DateTime DataExtratoConsolidado = new DateTime(UltimoDiaCompetencia.Year, UltimoDiaCompetencia.Month, UltimoDiaCompetencia.Day);
                DateTime DataInicioExtratoConsolidado = DataExtratoConsolidado.AddMonths(-1);
                foreach (DataRow RowConsolidadoContaCaixa in TableSaldoContaCaixa.AsEnumerable())
                {
                    if (RowConsolidadoContaCaixa != null)
                    {
                        int IdContaCaixaConsolidadora = Convert.ToInt32(RowConsolidadoContaCaixa["id_conta_caixa"]);
                        Decimal TotalPagoConsolidado = Decimal.Parse(RowConsolidadoContaCaixa["total_pago"].EmptyIfNull().ToString().Trim());
                        Decimal TotalRecebidoConsolidado = Decimal.Parse(RowConsolidadoContaCaixa["total_recebido"].EmptyIfNull().ToString().Trim());


                        g_contas_caixas ContaCaixaConsolidadora = ListaContasCaixas.Where(c => c.id_conta_caixa == IdContaCaixaConsolidadora).FirstOrDefault();
                        if (ContaCaixaConsolidadora != null)
                        {
                            if (ContaCaixaConsolidadora.tag_saldo_dia == "+") { SaldoFinalConsolidado += TotalRecebidoConsolidado - TotalPagoConsolidado; }
                            else if (ContaCaixaConsolidadora.tag_saldo_dia == "-") { SaldoFinalConsolidado += TotalRecebidoConsolidado + TotalPagoConsolidado; };
                        }
                    }
                }
                while (DataExtratoConsolidado >= DataInicioExtratoConsolidado)
                {
                    sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 1).SetCellValue(DataExtratoConsolidado.ToString("dd/MM/yyyy"));
                    if (IndexLinhaCC == 5)
                    {
                        sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 5).SetCellValue(Decimal.ToDouble(SaldoFinalConsolidado));
                    }
                    else
                    {
                        Decimal DiarioRecebidoConsolidado = 0;
                        Decimal DiarioPagoConsolidado = 0;
                        Decimal DiarioSaldoConsolidado = 0;
                        ListaExtratoConsolidadoContaCaixa = ListaExtratoContaCaixa.Where(c => c.data_pagamento == DataExtratoConsolidado).ToList();
                        foreach (CstRowExtratoContaCaixa RowConsolidadoContaCaixa in ListaExtratoConsolidadoContaCaixa)
                        {
                            g_contas_caixas ContaCaixaConsolidadora = ListaContasCaixas.Where(c => c.id_conta_caixa == RowConsolidadoContaCaixa.id_conta_caixa).FirstOrDefault();


                            if (ContaCaixaConsolidadora != null)
                            {
                                if (ContaCaixaConsolidadora.tag_saldo_dia == "+") 
                                {
                                    DiarioRecebidoConsolidado += RowConsolidadoContaCaixa.total_recebido;
                                    DiarioPagoConsolidado += RowConsolidadoContaCaixa.total_pago;
                                }
                                else if (ContaCaixaConsolidadora.tag_saldo_dia == "-") 
                                {
                                    DiarioRecebidoConsolidado -= RowConsolidadoContaCaixa.total_recebido;
                                    DiarioPagoConsolidado -= RowConsolidadoContaCaixa.total_pago;
                                };
                            }
                        }
                        DiarioSaldoConsolidado = DiarioRecebidoConsolidado - DiarioPagoConsolidado;
                        SaldoFinalConsolidado = SaldoFinalConsolidado - DiarioSaldoConsolidado;
                        sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 2).SetCellValue(Decimal.ToDouble(DiarioRecebidoConsolidado));
                        sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 3).SetCellValue(Decimal.ToDouble(DiarioPagoConsolidado));
                        sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 4).SetCellValue(Decimal.ToDouble(DiarioSaldoConsolidado));
                        sheetCatalogo.GetCell(IndexLinhaCC, IndexColunaCC + 5).SetCellValue(Decimal.ToDouble(SaldoFinalConsolidado));
                    }
                    IndexLinhaCC += 1;
                    DataExtratoConsolidado = DataExtratoConsolidado.AddDays(-1);
                }

                // Salvar o arquivo em disco
                String DirTempFiles = Server.MapPath("~/_filestemp");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "reports");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                FileNameExportacao = Path.Combine(DirTempFiles, "Relatório_Extrato_CC_" + UltimoDiaCompetencia.ToString("yyyyMMdd_HHmmss") + ".xls");
                FileStream fileStream = new FileStream(FileNameExportacao, FileMode.Create);
                using (FileStream FileSaida = fileStream)
                {
                    _workbookCatalogo.Write(FileSaida);
                    FileSaida.Close();
                    FileTemplate.Close();
                }

                // Atualizar o registro do processamento
                g_processamento record_g_processamento = new g_processamento();
                record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                record_g_processamento.detalhamento = "Relatório Lançamentos Financeiros";
                record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                record_g_processamento.datahora_inicio = PrimeiroDiaCompetencia;
                record_g_processamento.datahora_final = UltimoDiaCompetencia;
                record_g_processamento.qtd_registros = 30;
                record_g_processamento.qtd_reg_ok = 30;
                record_g_processamento.qtd_reg_erro = 0;
                record_g_processamento.processando = false;
                record_g_processamento.concluido = true;
                record_g_processamento.pathfile = FileNameExportacao;
                record_g_processamento.id_coligada = 1;
                record_g_processamento.id_filial = 1;
                db.g_processamento.Add(record_g_processamento);
                db.SaveChanges();

                IdProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                MsgRetorno = "Arquivo GERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                Sucesso = true;
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region jsAjaxPosicaoAtualContaCaixa
        [HttpPost]
        [GdiValidateAntiForgeryToken]
        public ActionResult AjaxPosicaoAtualContaCaixa(String parametros)
        {
            bool sucesso = false;
            String MsgRetorno = string.Empty;
            String MsgContasCaixasConsolidadas = string.Empty;
            String MsgContasCaixasAcessorias = string.Empty;
            Decimal SaldoTotalContasCaixasConsolidadas = 0;
            try
            {
                List<Db.g_contas_caixas> ListaContasCaixas = db.g_contas_caixas.Where(c => c.ativo == true && c.is_gerencial == true).OrderBy(c => c.ordem).ToList();
                String SqlSaldosContasCaixas = String.Empty;
                SqlSaldosContasCaixas += " select l.id_conta_caixa, ";
                SqlSaldosContasCaixas += "     SUM(CASE WHEN tipo_pag_rec = 1 THEN valor_pago ELSE 0 END) total_pago, ";
                SqlSaldosContasCaixas += "     SUM(CASE WHEN tipo_pag_rec = 2 THEN valor_pago ELSE 0 END) total_recebido ";
                SqlSaldosContasCaixas += " from gc_financeiro_lancamentos l ";
                SqlSaldosContasCaixas += " where (l.ativo = 1) and (id_financeiro_status != 3) and (id_financeiro_status != 5) and (l.data_pagamento >= '2022-06-01 00:00:00') ";
                SqlSaldosContasCaixas += " group by id_conta_caixa ";
                System.Data.DataTable TableSaldoContaCaixa = LibDB.GetDataTable(SqlSaldosContasCaixas, db);

                foreach (g_contas_caixas record_conta_caixa in ListaContasCaixas)
                {
                    Decimal TotalPago = 0;
                    Decimal TotalRecebido = 0;
                    Decimal SaldoContaCaixa = 0;
                    var RowSaldoContaCaixa = TableSaldoContaCaixa.AsEnumerable().Where(s => s.Field<int>("id_conta_caixa") == record_conta_caixa.id_conta_caixa).FirstOrDefault();
                    if (RowSaldoContaCaixa != null)
                    {
                        TotalPago = Decimal.Parse(RowSaldoContaCaixa["total_pago"].EmptyIfNull().ToString().Trim());
                        TotalRecebido = Decimal.Parse(RowSaldoContaCaixa["total_recebido"].EmptyIfNull().ToString().Trim());
                        SaldoContaCaixa = TotalRecebido - TotalPago;
                        if (record_conta_caixa.tag_saldo_dia.EmptyIfNull().ToString() == "+")
                        {
                            MsgContasCaixasConsolidadas += "(+)" + LibStringFormat.GetTabHtml(1) + record_conta_caixa.nome.EmptyIfNull().ToString() + ":" + LibStringFormat.GetTabHtml(1) + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", SaldoContaCaixa).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                            SaldoTotalContasCaixasConsolidadas += SaldoContaCaixa;
                        }
                        else if (record_conta_caixa.tag_saldo_dia.EmptyIfNull().ToString() == "-")
                        {
                            MsgContasCaixasConsolidadas += "(-)" + LibStringFormat.GetTabHtml(1) + record_conta_caixa.nome.EmptyIfNull().ToString() + ":" + LibStringFormat.GetTabHtml(1) + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", SaldoContaCaixa).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>"; ;
                            SaldoTotalContasCaixasConsolidadas -= SaldoContaCaixa;
                        }
                        else if (record_conta_caixa.tag_saldo_dia.EmptyIfNull().ToString() == "*") // Conta caixa acessória
                        {
                            MsgContasCaixasAcessorias += "(*)" + LibStringFormat.GetTabHtml(1) + record_conta_caixa.nome.EmptyIfNull().ToString() + ":" + LibStringFormat.GetTabHtml(1) + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", SaldoContaCaixa).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>"; ;
                        }
                    }
                }
                
                if (SaldoTotalContasCaixasConsolidadas > 0)
                {
                    MsgRetorno += LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "")  + LibStringFormat.GetTabHtml(1) + "<u>Contas Caixas Consolidadas - Posição Atual</u>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "") + "<br/><br/>";
                    MsgRetorno += MsgContasCaixasConsolidadas;
                    MsgRetorno += "------------------------------" + "<br/>";
                    MsgRetorno += "(=)" + LibStringFormat.GetTabHtml(1) + "Saldo Total:" + LibStringFormat.GetTabHtml(1) + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", SaldoTotalContasCaixasConsolidadas).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>"; ;
                }
                else
                {
                    MsgRetorno = LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "") + LibStringFormat.GetTabHtml(1) + "<u>Contas Caixas Consolidadas - Posição Atual</u>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "") + "<br/><br/>" + SaldoTotalContasCaixasConsolidadas;
                    MsgRetorno += "<br/>" + "------------------------------" + "<br/>";
                    MsgRetorno += "Não há Saldo nas Contas Caixas" + "<br/>"; ;
                }

                if (MsgContasCaixasAcessorias.EmptyIfNull().ToString().Length > 0)
                {
                    MsgRetorno += "<br/><br/>";
                    MsgRetorno += "<u>Contas Caixas Acessórias - Saldo</u>"+"<br/>";
                    MsgRetorno += "------------------------------" + "<br/>";
                    MsgRetorno += MsgContasCaixasAcessorias;
                }

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
            return Json(new { success = sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);

        }
        #endregion

        [HttpPost]
        [GdiValidateAntiForgeryToken]
        public ActionResult AjaxRoboItau()
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                /**/
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

        #region GetGedComex
        public ActionResult GetGedLancamentos(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            string filterOnOff = "0";
            try
            {
                bool filterDb = false;
                bool filterAdvanced = false;
                int IdLancamento = 0;
                int.TryParse(param.yesCustomIdPK, out IdLancamento);
                List<g_usuarios> allUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList();
                List<Db.ged_arquivos> allRecords = db.ged_arquivos.Where(g => g.ativo == true && g.id_gc_financeiro == IdLancamento).ToList();
                List<string[]> list = new List<string[]>();

                var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
                Func<Db.ged_arquivos, string> orderingFunction = (c =>
                                         param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_arquivo) :
                                         param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.filename :
                                         param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                         "");
                if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
                else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

                foreach (var ged in displayedRecords)
                {
                    String DataReferencia = String.Empty;
                    g_usuarios recordUsuario = allUsuarios.Where(u => u.id_usuario == ged.id_usuario_cadastro).FirstOrDefault();
                    String NomeUsuario = recordUsuario != null ? recordUsuario.login.EmptyIfNull().ToString() : string.Empty;
                    if (ged.datahora_cadastro != null) { DataReferencia = ged.datahora_cadastro.GetValueOrDefault().ToString("dd/MM/yy"); }

                    list.Add(new[] {
                                        ged.id_arquivo.ToString(),
                                        "",
                                        ged.descricao.ToString(),
                                        ged.filename.ToString(),
                                        DataReferencia,
                                        NomeUsuario,
                                        ""
                                    });
                }

                if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; }

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
                return JsonDataTableException(e, param, filterOnOff);
            }
        }
        #endregion

        #region ModalUploadAnexoFinanceiro
        public ActionResult ModalUploadAnexoFinanceiro(int? IdLancamento)
        {
            CstUploadGed record_cstUploadGed = new CstUploadGed();
            record_cstUploadGed.isLancamentoFinanceiro = true;
            gc_financeiro_lancamentos RecordLancamentoFinanceiro = db.gc_financeiro_lancamentos.Find(IdLancamento);
            if (RecordLancamentoFinanceiro == null || IdLancamento.GetValueOrDefault() <= 0)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Lançamento financeiro", IdLancamento);
                ViewBag.ComboGedTipos = new List<SelectListItem>();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Documentos (não localizado)</b>";
                return View(record_cstUploadGed);
            }
            record_cstUploadGed.id_gc_financeiro = RecordLancamentoFinanceiro.id_lancamento;
            record_cstUploadGed.id_gc_movimento = RecordLancamentoFinanceiro.id_movimento;
                var ComboGedTipos = new List<SelectListItem>();
                ComboGedTipos.Add(new SelectListItem { Value = "0", Text = "[ SELECIONE O TIPO DO ANEXO ]" });
                List<ged_arquivos_tipos> ListaGedTipos = db.ged_arquivos_tipos.Where(g => g.ativo == true && g.link_financeiro == true).OrderBy(p => p.descricao).ToList();
                foreach (var RecordGedTipo in ListaGedTipos) { ComboGedTipos.Add(new SelectListItem { Value = RecordGedTipo.id_arquivo_tipo.ToString(), Text = RecordGedTipo.descricao  }); };
                ViewBag.ComboGedTipos = ComboGedTipos;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Documentos</b>";
            return View(record_cstUploadGed);
        }
        #endregion

        #region Lançamentos Financeiros - Anexos - View (Modal)
        public ActionResult ModalFinanceiroViewAnexos(int? id, string tag)
        {
            gc_financeiro_lancamentos RecordFinanceiroLancamento = db.gc_financeiro_lancamentos.Find(id);
            if (RecordFinanceiroLancamento == null || id.GetValueOrDefault() <= 0)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Lançamento financeiro", id);
                RecordFinanceiroLancamento = new gc_financeiro_lancamentos { id_lancamento = id.GetValueOrDefault() };
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-paperclip", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Anexos do Lançamento Financeiro (não localizado)</b>";
                return View(RecordFinanceiroLancamento);
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-paperclip", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Anexos do Lançamento Financeiro Nº " + RecordFinanceiroLancamento.id_lancamento.EmptyIfNull().ToString() + "</b>";
            return View(RecordFinanceiroLancamento);
        }
        #endregion

        public ActionResult GetGedFinanceiro(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            string filterOnOff = "0";
            try
            {
                bool filterDb = false;
                bool filterAdvanced = false;
                int IdTable = 0;
                int.TryParse(param.yesCustomIdPK, out IdTable);
                List<g_usuarios> ListaUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList();
                List<ged_arquivos> ListaArquivosGed = db.ged_arquivos.Where(g => g.ativo == true && g.id_gc_financeiro == IdTable).ToList();
                List<ged_arquivos_tipos> ListaArquivosGedTipos = db.ged_arquivos_tipos.Where(t => t.id_arquivo_tipo > 0).ToList();
                List<string[]> list = new List<string[]>();

                var displayedRecords = ListaArquivosGed.Skip(param.iDisplayStart).Take(param.iDisplayLength);
                Func<Db.ged_arquivos, string> orderingFunction = (c =>
                                         param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_arquivo) :
                                         param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.filename :
                                         param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                         "");
                if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
                else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

                foreach (var RecordGed in displayedRecords)
                {
                    String DataReferencia = String.Empty;
                    String DescricaoTipoArquivo = String.Empty;
                    g_usuarios recordUsuario = ListaUsuarios.Where(u => u.id_usuario == RecordGed.id_usuario_cadastro).FirstOrDefault();
                    String NomeUsuario = recordUsuario != null ? recordUsuario.login.EmptyIfNull().ToString() : string.Empty;
                    if (RecordGed.datahora_cadastro != null) { DataReferencia = RecordGed.datahora_cadastro.GetValueOrDefault().ToString("dd/MM/yy"); }
                    if (RecordGed.id_arquivo_tipo > 0)
                    {
                        ged_arquivos_tipos RecordArquivoTipo = ListaArquivosGedTipos.Where(t => t.id_arquivo_tipo == RecordGed.id_arquivo_tipo).FirstOrDefault();
                        if (RecordArquivoTipo != null) { DescricaoTipoArquivo = RecordArquivoTipo.descricao.EmptyIfNull().ToString(); }
                    }

                    list.Add(new[] {
                                        RecordGed.id_arquivo.ToString(),
                                        "",
                                        DescricaoTipoArquivo.ToString(),
                                        RecordGed.descricao.ToString(),
                                        RecordGed.filename.ToString(),
                                        DataReferencia,
                                        NomeUsuario,
                                        ""
                                    });
                }

                if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = ListaArquivosGed.Count(),
                    iTotalDisplayRecords = ListaArquivosGed.Count(),
                    aaData = list
                },
                JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        #region AjaxFinanceiroBoletoGCPDF
        [HttpPost]
        [GdiValidateAntiForgeryToken]
        public ActionResult AjaxFinanceiroBoletoGCPDF(gc_financeiro_lancamentos view_record_gc_financeiro_lancamentos)
        {
            bool Sucesso = false;
            String MsgRetorno = "AjaxFinanceiroBoletoGCPDF";
            String idProcessamentoGravado = String.Empty;
            String SentencaSQLFinanceiro = String.Empty;
            String DirTempFiles = String.Empty;

            try
            {
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(view_record_gc_financeiro_lancamentos.id_lancamento);

                if (record_gc_financeiro_lancamentos == null)
                {
                    Sucesso = false;
                    MsgRetorno = "Lançamento financeiro [" + view_record_gc_financeiro_lancamentos.id_lancamento + "] não encontrado!";
                }
                else if (record_gc_financeiro_lancamentos.cnab_linha_digitavel.EmptyIfNull().ToString().Length == 0)
                {
                    Sucesso = false;
                    MsgRetorno = "Boleto [" + view_record_gc_financeiro_lancamentos.id_lancamento + "] não encontrado!";
                }
                else
                {
                    // Salvar o arquivo em disco
                    DateTime dataAtual = LibDateTime.getDataHoraBrasilia();

                    DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles);

                    gc_financeiro_lancamentos RecordFinanceiroLancamento = db.gc_financeiro_lancamentos.Find(view_record_gc_financeiro_lancamentos.id_lancamento);  
                    if (RecordFinanceiroLancamento != null)
                    {
                        g_clientes RecordCliente = db.g_clientes.Find(RecordFinanceiroLancamento.id_cliente);
                        g_cidades RecordCidade = db.g_cidades.Find(RecordCliente.id_cidade_com);
                        g_uf RecordUF = db.g_uf.Find(RecordCliente.id_uf_com);
                        g_contas_caixas RecordContaCaixaLancamento = db.g_contas_caixas.Find(RecordFinanceiroLancamento.id_conta_caixa);
                        g_contas_caixas RecordContaCaixaBancaria = db.g_contas_caixas.Find(RecordContaCaixaLancamento.id_conta_bancaria);

                        CstFinanceiroBoletos record_cstFinanceiroBoletos = new CstFinanceiroBoletos();
                        record_cstFinanceiroBoletos.idFinanceiro = view_record_gc_financeiro_lancamentos.id_lancamento;

                        // Cabeçalho
                        record_cstFinanceiroBoletos.EDadosCabecalho1 = "FATURA - PEDIDO Nº " + RecordFinanceiroLancamento.id_movimento.ToString().ToUpper();
                        record_cstFinanceiroBoletos.ECedenteNome = RecordContaCaixaBancaria.razao_social.EmptyIfNull().ToString().ToUpper();
                        record_cstFinanceiroBoletos.ECedenteComplemento1 = RecordContaCaixaBancaria.endereco_com.EmptyIfNull().ToString().ToUpper() + " " + RecordContaCaixaBancaria.bairro_com.EmptyIfNull().ToString().ToUpper() + " CEP: " + RecordContaCaixaBancaria.cep_com.EmptyIfNull().ToString().ToUpper();
                        record_cstFinanceiroBoletos.ECedenteComplemento2 = "BELO HORIZONTE - MG";

                        // Cliente
                        record_cstFinanceiroBoletos.EClienteNome = RecordCliente.nome.EmptyIfNull().ToString().ToUpper();
                        if (RecordCliente.cpf.EmptyIfNull().ToString().Trim() != String.Empty) { record_cstFinanceiroBoletos.EClienteDocumento = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("F", RecordCliente.cpf.EmptyIfNull().ToString().Trim()); }
                        else { record_cstFinanceiroBoletos.EClienteDocumento = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("J", RecordCliente.cnpj.EmptyIfNull().ToString().Trim()); }
                        record_cstFinanceiroBoletos.EClienteCodigo = RecordCliente.id_cliente.EmptyIfNull().ToString();
                        String _clienteEndereco = string.Empty;
                        String _clienteEnderecoCidadeUF = string.Empty;
                        _clienteEndereco += RecordCliente.endereco_com.EmptyIfNull().ToString().ToUpper() + " ";
                        _clienteEndereco += RecordCliente.bairro_com.EmptyIfNull().ToString().ToUpper() + " ";
                        _clienteEnderecoCidadeUF += RecordCidade.nome.EmptyIfNull().ToString().ToUpper() + " ";
                        _clienteEnderecoCidadeUF += RecordUF.nome.EmptyIfNull().ToString().ToUpper() + " ";
                        _clienteEnderecoCidadeUF += "CEP " + GdiPlataform.Lib.LibStringFormat.FormatarCEP(RecordCliente.cep_com.EmptyIfNull().ToString().ToUpper());

                        record_cstFinanceiroBoletos.EClienteEndereco = _clienteEndereco;
                        record_cstFinanceiroBoletos.EClienteEnderecoCidadeUF = _clienteEnderecoCidadeUF;

                        g_produtos extratoProdutoConsumoMinimo = new Db.g_produtos();
                        g_produtos extratoProdutoConsolidador = new Db.g_produtos();

                        // SubTotal
                        record_cstFinanceiroBoletos.EClienteMensagem = RecordContaCaixaBancaria.mensagem_cliente.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EValorLiquido = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(RecordFinanceiroLancamento.valor_total.EmptyIfNull().ToString()));
                        record_cstFinanceiroBoletos.EValorBruto = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(RecordFinanceiroLancamento.valor_total.EmptyIfNull().ToString()));

                        // MensagemCaixa
                        decimal ValorMulta = ((RecordFinanceiroLancamento.valor_total / 100) * 2);
                        decimal ValorJuros = ((RecordFinanceiroLancamento.valor_total / 100) * 1);
                        String MsgCaixa = String.Empty;
                        MsgCaixa += "Sr(a). Cliente pague esse título até o vencimento em toda a rede bancária, <br/> casas lotéricas, caixas eletrônicos, internet banking ou aplicativo do seu banco" + "<br><br>";
                        MsgCaixa += "O Não pagamento até o vencimento <b>" + RecordFinanceiroLancamento.data_vencimento.ToString("dd/MM/yy") + "</b> acarretará a cobrança de multa no valor de " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorMulta) + ",<br>"; ;
                        MsgCaixa += "Acrescido de juros diários de " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorJuros) + "." + "<br>";

                        // Boleto
                        if (RecordContaCaixaBancaria.banco.EmptyIfNull().ToString() == "341") { record_cstFinanceiroBoletos.ECodBanco = "341-7"; }
                        else if (RecordContaCaixaBancaria.banco.EmptyIfNull().ToString() == "237") { record_cstFinanceiroBoletos.ECodBanco = "237-0"; }
                        else { record_cstFinanceiroBoletos.ECodBanco = RecordContaCaixaBancaria.banco.EmptyIfNull().ToString(); }
                        record_cstFinanceiroBoletos.ELogoBanco = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/banco" + record_gc_financeiro_lancamentos.boleto_banco.EmptyIfNull().ToString() + ".png";
                        record_cstFinanceiroBoletos.ELogoBoletoBanco = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/LogoBoleto" + record_gc_financeiro_lancamentos.boleto_banco.EmptyIfNull().ToString() + ".png";
                        record_cstFinanceiroBoletos.EDataVencimento = RecordFinanceiroLancamento.data_vencimento.ToString("dd/MM/yy");
                        record_cstFinanceiroBoletos.ECedenteNome = RecordContaCaixaBancaria.razao_social.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ECedenteCNPJ = RecordContaCaixaBancaria.cnpj.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ECedenteComplemento1 = RecordContaCaixaBancaria.endereco_com.EmptyIfNull().ToString().ToUpper() + " " + RecordContaCaixaBancaria.bairro_com.EmptyIfNull().ToString().ToUpper();
                        record_cstFinanceiroBoletos.ECedenteComplemento2 = " CEP: " + RecordContaCaixaBancaria.cep_com.EmptyIfNull().ToString().ToUpper() + " CNPJ: " + GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("J", RecordContaCaixaBancaria.cnpj.EmptyIfNull().ToString().ToUpper());
                        record_cstFinanceiroBoletos.EAgenciaCodCedente = GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularAgenciaCodigoCedente(RecordContaCaixaBancaria.banco.EmptyIfNull().ToString(), RecordContaCaixaBancaria.agencia.EmptyIfNull().ToString(), RecordContaCaixaBancaria.dv_agencia.EmptyIfNull().ToString(), RecordContaCaixaBancaria.conta.EmptyIfNull().ToString(), RecordContaCaixaBancaria.dv_conta.EmptyIfNull().ToString(), RecordContaCaixaBancaria.codigo_convenio.EmptyIfNull().ToString());
                        record_cstFinanceiroBoletos.EDataDocumento = RecordFinanceiroLancamento.data_vencimento.ToString("dd/MM/yy");
                        record_cstFinanceiroBoletos.ENossoNumeroDV = RecordFinanceiroLancamento.cnab_nosso_numero.EmptyIfNull().ToString();
                        if (RecordFinanceiroLancamento.numero_documento.EmptyIfNull().ToString().Length > 0) { record_cstFinanceiroBoletos.ENumeroDocumento = "NF " + RecordFinanceiroLancamento.numero_documento.EmptyIfNull().ToString(); } else { record_cstFinanceiroBoletos.ENumeroDocumento = RecordFinanceiroLancamento.descricao.EmptyIfNull().ToString(); }
                        record_cstFinanceiroBoletos.EEspecieDoc = "NS";
                        record_cstFinanceiroBoletos.EAceite = "N";
                        record_cstFinanceiroBoletos.EDataProcessamento = RecordFinanceiroLancamento.data_vencimento.ToString("dd/MM/yy");
                        record_cstFinanceiroBoletos.ENossoNumeroDV = RecordFinanceiroLancamento.cnab_nosso_numero.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ECarteira = RecordContaCaixaBancaria.carteira.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EEspecieMoeda = RecordContaCaixaBancaria.especie_moeda.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EValorTotal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordFinanceiroLancamento.valor_total);
                        record_cstFinanceiroBoletos.EMensagemCaixa = MsgCaixa;
                        record_cstFinanceiroBoletos.ENomeSacado = RecordCliente.nome.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EEnderecoSacado = RecordCliente.endereco_com.EmptyIfNull().ToString() + " " + RecordCliente.bairro_com.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ECidadeSacado = RecordCidade.nome.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ECepSacado = GdiPlataform.Lib.LibStringFormat.FormatarCEP(RecordCliente.cep_com.EmptyIfNull().ToString());
                        record_cstFinanceiroBoletos.EUFSacado = RecordUF.nome.EmptyIfNull().ToString();
                        if (RecordCliente.cpf.EmptyIfNull().ToString().Trim() != String.Empty)
                        { record_cstFinanceiroBoletos.EDocSacado = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("F", RecordCliente.cpf.EmptyIfNull().ToString().Trim()); }
                        else { record_cstFinanceiroBoletos.EDocSacado = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("J", RecordCliente.cnpj.EmptyIfNull().ToString().Trim()); }
                        record_cstFinanceiroBoletos.ELinhaDigitavel = GdiPlataform.Lib.LibStringFormat.FormatarLinhaDigitavel(RecordFinanceiroLancamento.cnab_linha_digitavel.EmptyIfNull().ToString());
                        record_cstFinanceiroBoletos.ECodigoBarras = GdiPlataform.Lib.LibCnabBancario.GetCodigoBarras(RecordFinanceiroLancamento.cnab_linha_digitavel);
                        record_cstFinanceiroBoletos.EImgBarCode = LibBoletos.Generate_barcode(RecordFinanceiroLancamento.cnab_codigo_barras, RecordFinanceiroLancamento.data_vencimento, Server.MapPath("~/_filestemp"));
                        if (RecordFinanceiroLancamento.pix_base64.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            record_cstFinanceiroBoletos.HasPix = true;
                            record_cstFinanceiroBoletos.EPixEMV = RecordFinanceiroLancamento.pix_emv.EmptyIfNull().ToString().Trim().Replace(" ", "&nbsp;");
                            record_cstFinanceiroBoletos.EImgPixQrCode = LibBoletos.Generate_PixQrCode(RecordFinanceiroLancamento.id_lancamento, RecordFinanceiroLancamento.pix_base64.EmptyIfNull().ToString().Trim(), RecordFinanceiroLancamento.data_vencimento, Server.MapPath("~/_filestemp"));
                        }
                        ;
                        ViewBag.Title = "Boleto - " + record_cstFinanceiroBoletos.EClienteNome.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.printPDF = true;

                        String TemplateBoleto = "BoletoPdfFebraban";
                        var pdf = new ViewAsPdf
                        {
                            ViewName = TemplateBoleto,
                            Model = record_cstFinanceiroBoletos
                        };

                        // Criar o PDF
                        byte[] applicationPDFData = pdf.BuildFile(ControllerContext);
                        string fileNamePDF1 = "Boleto - " + LibStringFormat.SomenteAlfabetoeNumeros(record_cstFinanceiroBoletos.EClienteNome.ToString()) + ".pdf";
                        fileNamePDF1 = Path.Combine(DirTempFiles, fileNamePDF1);
                        var fileStream = new FileStream(fileNamePDF1, FileMode.Create, FileAccess.Write);
                        fileStream.Write(applicationPDFData, 0, applicationPDFData.Length);
                        fileStream.Close();
                        listaPdfsGerados.Add(fileNamePDF1);

                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 37;
                        record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                        record_g_processamento.datahora_inicio = LibDateTime.getDataHoraBrasilia();
                        record_g_processamento.datahora_final = record_g_processamento.datahora_inicio;
                        record_g_processamento.qtd_registros = 1;
                        record_g_processamento.qtd_reg_ok = 1;
                        record_g_processamento.qtd_reg_erro = 0;
                        record_g_processamento.processando = false;
                        record_g_processamento.concluido = true;
                        record_g_processamento.pathfile = fileNamePDF1;
                        record_g_processamento.id_coligada = 1;
                        record_g_processamento.id_filial = 1;
                        db.g_processamento.Add(record_g_processamento);
                        db.SaveChanges();
                        idProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                        MsgRetorno = "Boleto Gerado com Sucesso!";
                        Sucesso = true;
                    }
                    else
                    {
                        Sucesso = false;
                        MsgRetorno = "Boleto [" + view_record_gc_financeiro_lancamentos.id_lancamento + "] não encontrado!";
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
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }
        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }

    }
} 