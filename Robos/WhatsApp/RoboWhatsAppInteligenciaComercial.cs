using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using GdiPlataform.Db;
using GdiPlataform.Lib;

namespace GdiPlataform.Robos.Whatsapp
{
    // Envia, individualmente a cada vendedor, um relatório de Inteligência Comercial via WhatsApp (Z-API).
    // Executado em background pelo JobServerController — sem dependência de HttpContext/CachePersister.
    // Destinatários: g_vendedores com report_inteligencia_comercial = true (envio para telefone_1).
    // Métricas por vendedor:
    //   1) Comissões confirmadas no mês corrente (mesma lógica de ModalRelatorioVendedoresComissoes).
    //   2) Total de parcelas financeiras atrasadas dos pedidos (lógica de ModalRelatorioVendedoresAtrasados).
    //   3) Total de pedidos fechados no mês corrente (lógica de ModalRelatorioVendedoresPedidos).
    //   4/5/6) Total de cotações em aberto por faixa de datahora_alteracao (0-30 / 30-60 / 60-90 dias).
    public static class RoboWhatsAppInteligenciaComercial
    {
        private const string INSTANCE_ID    = "3D933D08BC343053ACA9CE82F470C0C7";
        private const string INSTANCE_TOKEN = "ACE6A8DDC0AF992D50FDD712";
        private const string CLIENT_TOKEN   = "F473838c008c242fc9ae05bf7b4727c37S";

        // Início de vigência do pagamento de comissões (espelha os relatórios comerciais).
        private static readonly DateTime DataInicioPagtoComissao = new DateTime(2025, 1, 1);

        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        public static void EnviarRelatorioInteligenciaComercial(string jobId, string parameters,
            System.Threading.CancellationToken cancellationToken, string database)
        {
            LibLogger.Info($"[JobServer] EnviarRelatorioInteligenciaComercialWhatsApp iniciando | JobId: {jobId}");

            using (var db = new GdiPlataformEntities(database))
            {
                g_jobserver recordJob = new g_jobserver
                {
                    job_id          = jobId,
                    job_name        = "EnviarRelatorioInteligenciaComercialWhatsApp",
                    job_parameters  = parameters,
                    datahora_inicio = DateTime.Now
                };
                db.g_jobserver.Add(recordJob);
                db.SaveChanges();

                try
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    // parameters opcional: telefone de teste (DDI 55). Se informado e válido, todos os
                    // relatórios são enviados para esse número (modo homologação), em vez do telefone_1 do vendedor.
                    string telefoneTeste = NormalizarTelefoneValido(parameters);

                    var vendedores = db.g_vendedores.AsNoTracking()
                        .Where(v => v.report_inteligencia_comercial == true)
                        .Select(v => new { v.id_vendedor, v.nome, v.telefone_1 })
                        .ToList();

                    LibLogger.Info($"[JobServer] InteligenciaComercial | vendedores parametrizados: {vendedores.Count}");

                    if (vendedores.Count == 0)
                    {
                        recordJob.qtd_rows_sucesso = 0;
                        recordJob.concluido        = true;
                        recordJob.datahora_fim     = DateTime.Now;
                        db.Entry(recordJob).State  = EntityState.Modified;
                        db.SaveChanges();
                        return;
                    }

                    var idsVendedores = vendedores.Select(v => v.id_vendedor).Distinct().ToList();

                    var comissoesConfirmadas = CalcularComissoesConfirmadasMes(db, idsVendedores);
                    var parcelasAtrasadas    = CalcularParcelasAtrasadas(db, idsVendedores);
                    var pedidosFechadosMes   = CalcularPedidosFechadosMes(db, idsVendedores);
                    CalcularCotacoesEmAberto(db, idsVendedores,
                        out var cotacoes0a30, out var cotacoes30a60, out var cotacoes60a90);

                    DateTime dataHora = LibDateTime.getDataHoraBrasilia();

                    var falhas = new List<string>();
                    int enviados = 0;

                    foreach (var vendedor in vendedores)
                    {
                        string celular = telefoneTeste ?? NormalizarTelefoneValido(vendedor.telefone_1);
                        if (string.IsNullOrWhiteSpace(celular))
                        {
                            falhas.Add($"Vendedor {vendedor.id_vendedor} ({vendedor.nome}): telefone_1 inválido/ausente.");
                            continue;
                        }

                        string mensagem = MontarMensagem(
                            dataHora,
                            vendedor.nome,
                            ValorDe(comissoesConfirmadas, vendedor.id_vendedor),
                            ValorDe(parcelasAtrasadas, vendedor.id_vendedor),
                            ValorDe(pedidosFechadosMes, vendedor.id_vendedor),
                            ValorDe(cotacoes0a30, vendedor.id_vendedor),
                            ValorDe(cotacoes30a60, vendedor.id_vendedor),
                            ValorDe(cotacoes60a90, vendedor.id_vendedor));

                        try
                        {
                            EnviarZApi(celular, mensagem);
                            enviados++;
                        }
                        catch (Exception ex)
                        {
                            falhas.Add($"Vendedor {vendedor.id_vendedor} ({vendedor.nome}) [{MascararCelularLog(celular)}]: {LibExceptions.getExceptionShortMessage(ex)}");
                        }
                    }

                    recordJob.qtd_rows_sucesso = enviados;
                    recordJob.qtd_rows_erro    = falhas.Count;
                    recordJob.concluido        = falhas.Count == 0;
                    recordJob.datahora_fim     = DateTime.Now;
                    db.Entry(recordJob).State  = EntityState.Modified;
                    db.SaveChanges();

                    if (falhas.Count > 0)
                    {
                        throw new Exception(
                            "Relatório Inteligência Comercial WhatsApp: " + enviados + " enviado(s), " +
                            falhas.Count + " falha(s). " + string.Join(" | ", falhas));
                    }

                    LibLogger.Info($"[JobServer] EnviarRelatorioInteligenciaComercialWhatsApp concluído | JobId: {jobId} | Enviados: {enviados}");
                }
                catch (Exception ex)
                {
                    LibLogger.Error($"[JobServer] EnviarRelatorioInteligenciaComercialWhatsApp erro | JobId: {jobId} | {LibExceptions.getExceptionShortMessage(ex)}", ex);

                    try
                    {
                        recordJob.concluido       = false;
                        recordJob.datahora_fim    = DateTime.Now;
                        db.Entry(recordJob).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    catch { /* falha de DB no log de erro não deve suprimir a exceção original */ }

                    throw;
                }
            }
        }

        // =====================================================================
        // 1) Comissões confirmadas no mês corrente (por comissao1_vendedor).
        //    Espelha AjaxModalRelatorioVendedoresComissoes(Projetado), período 1 (mês corrente).
        // =====================================================================
        private static Dictionary<int, decimal> CalcularComissoesConfirmadasMes(GdiPlataformEntities db, List<int> idsVendedores)
        {
            var resultado = new Dictionary<int, decimal>();

            DateTime dataInicial = LibDateTime.getPrimeiroDiaMesAtual().Date;
            DateTime dataFinal   = LibDateTime.getUltimoDiaMesAtual().Date;
            DateTime dataInicialSQL = new DateTime(dataInicial.Year, dataInicial.Month, dataInicial.Day, 0, 0, 0);
            DateTime dataFinalSQL   = new DateTime(dataFinal.Year, dataFinal.Month, dataFinal.Day, 23, 59, 59);

            var lancamentos = db.gc_financeiro_lancamentos.AsNoTracking()
                .Where(l => l.ativo == true && l.tipo_pag_rec == 2 && l.id_movimento > 0
                            && l.is_provisao_imposto == false && l.is_difal == false
                            && (l.id_financeiro_status == 1 || l.id_financeiro_status == 4)
                            && l.data_vencimento >= dataInicialSQL && l.data_vencimento <= dataFinalSQL)
                .Select(l => new
                {
                    l.id_movimento,
                    l.id_financeiro_status,
                    l.valor_pago,
                    l.valor_total,
                    l.id_lancamento_adiantamento
                })
                .ToList();

            if (lancamentos.Count == 0) return resultado;

            var idsMovimentos = lancamentos.Select(l => l.id_movimento).Distinct().ToList();
            var movimentos = db.gc_movimentos.AsNoTracking()
                .Where(m => idsMovimentos.Contains(m.id_movimento)
                            && m.comissao1_vendedor > 0
                            && idsVendedores.Contains(m.comissao1_vendedor)
                            && m.datahora_aprovacao >= DataInicioPagtoComissao)
                .Select(m => new
                {
                    m.id_movimento,
                    m.comissao1_vendedor,
                    m.comissao1_percentual,
                    m.valor_total_bruto,
                    m.frete_valor,
                    m.frete_gerencial
                })
                .ToDictionary(m => m.id_movimento);

            foreach (var lanc in lancamentos)
            {
                if (!movimentos.TryGetValue(lanc.id_movimento, out var mov)) continue;
                if (mov.valor_total_bruto <= 0) continue;

                decimal valorLiquidoPedido = mov.valor_total_bruto - mov.frete_valor - mov.frete_gerencial;
                decimal valorComissaoPedido = (valorLiquidoPedido / 100) * mov.comissao1_percentual;
                if (valorComissaoPedido <= 0) continue;

                decimal valorPago = lanc.valor_pago;
                if (valorPago == 0 && lanc.id_lancamento_adiantamento > 0) valorPago = lanc.valor_total;

                decimal percentualComissionadoValorBruto = (valorLiquidoPedido * 100) / mov.valor_total_bruto;
                decimal valorComissionadoLancamento = (valorPago / 100) * percentualComissionadoValorBruto;
                decimal valorComissaoLancamento = (valorComissionadoLancamento / 100) * mov.comissao1_percentual;

                Acumular(resultado, mov.comissao1_vendedor, valorComissaoLancamento);
            }

            return resultado;
        }

        // =====================================================================
        // 2) Total de parcelas financeiras atrasadas dos pedidos (por comissao1_vendedor).
        //    Espelha o critério de AjaxModalRelatorioVendedoresAtrasados (status 3, venc. <= hoje-3),
        //    somando o valor dos títulos (valor_total), conforme solicitado.
        // =====================================================================
        private static Dictionary<int, decimal> CalcularParcelasAtrasadas(GdiPlataformEntities db, List<int> idsVendedores)
        {
            var resultado = new Dictionary<int, decimal>();

            DateTime dataCorte = LibDateTime.getDataHoraBrasilia().AddDays(-3);
            DateTime dataFinalSQL = new DateTime(dataCorte.Year, dataCorte.Month, dataCorte.Day, 23, 59, 59);

            var lancamentos = db.gc_financeiro_lancamentos.AsNoTracking()
                .Where(l => l.ativo == true && l.tipo_pag_rec == 2 && l.id_movimento > 0
                            && l.is_provisao_imposto == false && l.is_difal == false
                            && l.id_financeiro_status == 3
                            && l.data_vencimento <= dataFinalSQL)
                .Select(l => new { l.id_movimento, l.valor_total })
                .ToList();

            if (lancamentos.Count == 0) return resultado;

            var idsMovimentos = lancamentos.Select(l => l.id_movimento).Distinct().ToList();
            var movimentos = db.gc_movimentos.AsNoTracking()
                .Where(m => idsMovimentos.Contains(m.id_movimento)
                            && m.comissao1_vendedor > 0
                            && idsVendedores.Contains(m.comissao1_vendedor))
                .Select(m => new { m.id_movimento, m.comissao1_vendedor })
                .ToDictionary(m => m.id_movimento, m => m.comissao1_vendedor);

            foreach (var lanc in lancamentos)
            {
                if (!movimentos.TryGetValue(lanc.id_movimento, out int idVendedor)) continue;
                Acumular(resultado, idVendedor, lanc.valor_total);
            }

            return resultado;
        }

        // =====================================================================
        // 3) Total de pedidos fechados no mês corrente (por comissao1_vendedor).
        //    Espelha o filtro de ModalRelatorioVendedoresPedidos (NF autorizada + COALESCE datahora_nf).
        // =====================================================================
        private static Dictionary<int, decimal> CalcularPedidosFechadosMes(GdiPlataformEntities db, List<int> idsVendedores)
        {
            var resultado = new Dictionary<int, decimal>();

            DateTime dataInicial = LibDateTime.getPrimeiroDiaMesAtual();
            DateTime dataFinal   = LibDateTime.getUltimoDiaMesAtual();

            string sql =
                " SELECT m.comissao1_vendedor AS id_vendedor, SUM(m.valor_total_bruto) AS valor" +
                " FROM gc_movimentos m" +
                " JOIN gc_cfop_operacoes cf ON cf.id_cfop_operacao = m.id_cfop_operacao" +
                " WHERE m.movimento_aprovado = 1" +
                "   AND COALESCE(m.datahora_nf, (" +
                "       SELECT MIN(nf_inner.nf_data_autorizacao) FROM gc_movimentos_nf nf_inner" +
                "       WHERE nf_inner.id_movimento = m.id_movimento" +
                "         AND nf_inner.id_nfe_status IN (8, 17, 22)" +
                "         AND nf_inner.nf_data_autorizacao IS NOT NULL" +
                "   )) BETWEEN" +
                "       '" + dataInicial.ToString("yyyy-MM-dd 00:00:00") + "'" +
                "       AND '" + dataFinal.ToString("yyyy-MM-dd 23:59:59") + "'" +
                "   AND cf.is_venda = 1" +
                "   AND m.comissao1_vendedor > 0" +
                "   AND EXISTS (" +
                "       SELECT 1 FROM gc_movimentos_nf nf" +
                "       WHERE nf.id_movimento = m.id_movimento" +
                "         AND nf.id_nfe_status IN (8, 17, 22)" +
                "   )" +
                " GROUP BY m.comissao1_vendedor";

            DataTable table = LibDB.GetDataTable(sql, db);
            foreach (DataRow row in table.AsEnumerable())
            {
                int.TryParse(row["id_vendedor"].EmptyIfNull().ToString(), out int idVendedor);
                decimal.TryParse(row["valor"].EmptyIfNull().ToString(), out decimal valor);
                if (idsVendedores.Contains(idVendedor))
                    Acumular(resultado, idVendedor, valor);
            }

            return resultado;
        }

        // =====================================================================
        // 4/5/6) Total de cotações em aberto (id_movimento_tipo=3, status=1, não cancelada/devolvida)
        //        por faixa de datahora_alteracao (0-30 / 30-60 / 60-90 dias), por id_vendedor.
        // =====================================================================
        private static void CalcularCotacoesEmAberto(GdiPlataformEntities db, List<int> idsVendedores,
            out Dictionary<int, decimal> cotacoes0a30,
            out Dictionary<int, decimal> cotacoes30a60,
            out Dictionary<int, decimal> cotacoes60a90)
        {
            cotacoes0a30  = new Dictionary<int, decimal>();
            cotacoes30a60 = new Dictionary<int, decimal>();
            cotacoes60a90 = new Dictionary<int, decimal>();

            DateTime hoje  = LibDateTime.getDataHoraBrasilia().Date;
            DateTime lim90 = hoje.AddDays(-90);

            var cotacoes = db.gc_movimentos.AsNoTracking()
                .Where(m => m.id_movimento_tipo == 3 && m.id_movimento_status == 1
                            && m.movimento_cancelado == false && m.movimento_devolvido == false
                            && idsVendedores.Contains(m.id_vendedor)
                            && m.datahora_alteracao != null
                            && m.datahora_alteracao >= lim90)
                .Select(m => new { m.id_vendedor, m.valor_total_bruto, m.datahora_alteracao })
                .ToList();

            foreach (var cot in cotacoes)
            {
                if (cot.datahora_alteracao == null) continue;
                int dias = (hoje - cot.datahora_alteracao.Value.Date).Days;
                if (dias < 0) dias = 0;

                if (dias <= 30) Acumular(cotacoes0a30, cot.id_vendedor, cot.valor_total_bruto);
                else if (dias <= 60) Acumular(cotacoes30a60, cot.id_vendedor, cot.valor_total_bruto);
                else if (dias <= 90) Acumular(cotacoes60a90, cot.id_vendedor, cot.valor_total_bruto);
            }
        }

        private static void Acumular(Dictionary<int, decimal> mapa, int idVendedor, decimal valor)
        {
            if (mapa.ContainsKey(idVendedor)) mapa[idVendedor] += valor;
            else mapa[idVendedor] = valor;
        }

        private static decimal ValorDe(Dictionary<int, decimal> mapa, int idVendedor)
        {
            return mapa.ContainsKey(idVendedor) ? mapa[idVendedor] : 0m;
        }

        private static string MontarMensagem(
            DateTime dataHora,
            string nomeVendedor,
            decimal comissoesConfirmadas,
            decimal parcelasAtrasadas,
            decimal pedidosFechadosMes,
            decimal cotacoes0a30,
            decimal cotacoes30a60,
            decimal cotacoes60a90)
        {
            var sb = new StringBuilder();
            sb.Append("✈️ GDI - Inteligência Comercial\n");
            sb.Append("📅 " + dataHora.ToString("dd/MM/yyyy") + " | " + dataHora.ToString("HH:mm") + "\n");
            sb.Append("Olá, " + (nomeVendedor ?? "").Trim() + "!\n");
            sb.Append("\n");
            sb.Append("💰 Comissões confirmadas (mês)\n");
            sb.Append("R$ " + comissoesConfirmadas.ToString("N2", PtBR) + "\n");
            sb.Append("\n");
            sb.Append("⚠️ Parcelas atrasadas\n");
            sb.Append("R$ " + parcelasAtrasadas.ToString("N2", PtBR) + "\n");
            sb.Append("\n");
            sb.Append("📦 Pedidos fechados (mês)\n");
            sb.Append("R$ " + pedidosFechadosMes.ToString("N2", PtBR) + "\n");
            sb.Append("\n");
            sb.Append("📋 Cotações em aberto\n");
            sb.Append("0-30 dias   R$ " + cotacoes0a30.ToString("N2", PtBR) + "\n");
            sb.Append("30-60 dias  R$ " + cotacoes30a60.ToString("N2", PtBR) + "\n");
            sb.Append("60-90 dias  R$ " + cotacoes60a90.ToString("N2", PtBR) + "\n");

            return sb.ToString().TrimEnd('\n');
        }

        private static string NormalizarTelefoneValido(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone)) return null;
            string normalizado = LibStringFormat.NormalizarTelefoneWhatsAppBrasil(telefone.Trim());
            if (string.IsNullOrWhiteSpace(normalizado) || normalizado.Length < 12 || normalizado.Length > 13)
                return null;
            if (!normalizado.StartsWith("55", StringComparison.Ordinal))
                return null;
            return normalizado;
        }

        private static string MascararCelularLog(string celular)
        {
            if (string.IsNullOrEmpty(celular) || celular.Length <= 4)
                return "****";
            return "****" + celular.Substring(celular.Length - 4);
        }

        private static void EnviarZApi(string celular, string mensagem)
        {
            string url = "https://api.z-api.io/instances/" + INSTANCE_ID + "/token/" + INSTANCE_TOKEN;
            var client  = new RestClient(url);
            var request = new RestRequest("/send-text", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("client-token", CLIENT_TOKEN);

            string json = JsonConvert.SerializeObject(new { phone = celular, message = mensagem });
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = client.Execute(request);
            LibLogger.Info($"[JobServer] Z-API response | Status: {(int)response.StatusCode} | Body: {response.Content}");

            if (!response.IsSuccessful)
                throw new Exception($"Z-API retornou erro HTTP {(int)response.StatusCode}: {response.Content}");

            ValidarRespostaZApi(response.Content);
        }

        private static void ValidarRespostaZApi(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                return;

            JObject json;
            try
            {
                json = JObject.Parse(responseContent);
            }
            catch (JsonReaderException)
            {
                return;
            }

            if (json["error"] != null && !string.IsNullOrWhiteSpace(json["error"].ToString()))
                throw new Exception("Z-API rejeitou o envio: " + json["error"]);

            if (json["success"] != null && json["success"].Type == JTokenType.Boolean && !json["success"].Value<bool>())
            {
                string msg = json["message"]?.ToString();
                throw new Exception("Z-API rejeitou o envio: " + (string.IsNullOrWhiteSpace(msg) ? responseContent : msg));
            }
        }
    }
}
