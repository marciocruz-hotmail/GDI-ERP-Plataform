using Newtonsoft.Json;
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
    // Envia resumo gerencial diário via WhatsApp (Z-API).
    // Executado em background pelo JobServerController — sem dependência de HttpContext/CachePersister.
    public static class RoboWhatsAppGerencial
    {
        private const string DESTINATARIO   = "5531985113025";
        private const string INSTANCE_ID    = "3D933D08BC343053ACA9CE82F470C0C7";
        private const string INSTANCE_TOKEN = "ACE6A8DDC0AF992D50FDD712";
        private const string CLIENT_TOKEN   = "F473838c008c242fc9ae05bf7b4727c37S";

        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        // id_vendedor → nome exibição (SC = Paulo, id 6)
        private static readonly Dictionary<int, string> NomesVendedor = new Dictionary<int, string>
        {
            {  4, "André"   },
            {  8, "Carlos"  },
            {  1, "Daniel"  },
            {  2, "Gustavo" },
            {  3, "João"    },
            {  6, "Paulo C." },
            {  7, "Vivian"  },
            {  9, "Déborah" },
            { 10, "Leo"     }
        };

        private static readonly int[] IdsVendedoresOrdemAlfabetica = NomesVendedor
            .OrderBy(kv => kv.Value, StringComparer.Create(PtBR, ignoreCase: true))
            .Select(kv => kv.Key)
            .ToArray();

        public static void EnviarResumoGerencial(string jobId, string parameters,
            System.Threading.CancellationToken cancellationToken, string database)
        {
            LibLogger.Info($"[JobServer] EnviarResumoGerencialWhatsApp iniciando | JobId: {jobId}");

            using (var db = new GdiPlataformEntities(database))
            {
                g_jobserver recordJob = new g_jobserver
                {
                    job_id         = jobId,
                    job_name       = "EnviarResumoGerencialWhatsApp",
                    job_parameters = parameters,
                    datahora_inicio = DateTime.Now
                };
                db.g_jobserver.Add(recordJob);
                db.SaveChanges();

                try
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    string mensagem = MontarMensagem(db);
                    EnviarZApi(DESTINATARIO, mensagem);

                    recordJob.qtd_rows_sucesso = 1;
                    recordJob.concluido        = true;
                    recordJob.datahora_fim     = DateTime.Now;
                    db.Entry(recordJob).State  = EntityState.Modified;
                    db.SaveChanges();

                    LibLogger.Info($"[JobServer] EnviarResumoGerencialWhatsApp concluído | JobId: {jobId}");
                }
                catch (Exception ex)
                {
                    LibLogger.Error($"[JobServer] EnviarResumoGerencialWhatsApp erro | JobId: {jobId}", ex);

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

        private static string MontarMensagem(GdiPlataformEntities db)
        {
            DateTime dataHora = LibDateTime.getDataHoraBrasilia();

            string sqlDiario =
                " SELECT g_vendedores.id_vendedor," +
                "        COUNT(*) AS qtd," +
                "        SUM(gc_movimentos.valor_total_bruto) AS valor" +
                " FROM gc_movimentos" +
                " INNER JOIN gc_movimentos_nf" +
                "         ON gc_movimentos_nf.id_movimento = gc_movimentos.id_movimento" +
                "        AND gc_movimentos_nf.id_nfe_status IN (8, 17, 22)" +
                " INNER JOIN g_vendedores" +
                "         ON gc_movimentos.id_vendedor = g_vendedores.id_vendedor" +
                " INNER JOIN gc_cfop_operacoes" +
                "         ON gc_cfop_operacoes.id_cfop_operacao = gc_movimentos.id_cfop_operacao" +
                " WHERE gc_movimentos.movimento_aprovado = 1" +
                "   AND gc_movimentos.datahora_aprovacao BETWEEN" +
                "       '" + dataHora.ToString("yyyy-MM-dd 00:00:00") + "'" +
                "       AND '" + dataHora.ToString("yyyy-MM-dd 23:59:59") + "'" +
                "   AND gc_cfop_operacoes.is_venda = 1" +
                " GROUP BY g_vendedores.id_vendedor, g_vendedores.nome";

            AgregarPedidosPorVendedor(LibDB.GetDataTable(sqlDiario, db),
                out var qtdDiarioPorVendedor, out var valorDiarioPorVendedor,
                out var qtdGDIHoje, out var valorGDIHoje, out var qtdSCHoje, out var valorSCHoje);

            string sqlMes =
                " SELECT g_vendedores.id_vendedor," +
                "        COUNT(*) AS qtd," +
                "        SUM(gc_movimentos.valor_total_bruto) AS valor" +
                " FROM gc_movimentos" +
                " INNER JOIN gc_movimentos_nf" +
                "         ON gc_movimentos_nf.id_movimento = gc_movimentos.id_movimento" +
                "        AND gc_movimentos_nf.id_nfe_status IN (8, 17, 22)" +
                " INNER JOIN g_vendedores" +
                "         ON gc_movimentos.id_vendedor = g_vendedores.id_vendedor" +
                " INNER JOIN gc_cfop_operacoes" +
                "         ON gc_cfop_operacoes.id_cfop_operacao = gc_movimentos.id_cfop_operacao" +
                " WHERE gc_movimentos.movimento_aprovado = 1" +
                "   AND gc_movimentos.datahora_aprovacao BETWEEN" +
                "       '" + LibDateTime.getPrimeiroDiaMesAtual().ToString("yyyy-MM-dd 00:00:00") + "'" +
                "       AND '" + LibDateTime.getUltimoDiaMesAtual().ToString("yyyy-MM-dd 23:59:59") + "'" +
                "   AND gc_cfop_operacoes.is_venda = 1" +
                " GROUP BY g_vendedores.id_vendedor, g_vendedores.nome";

            AgregarPedidosPorVendedor(LibDB.GetDataTable(sqlMes, db),
                out var qtdMesPorVendedor, out var valorMesPorVendedor,
                out var qtdGDIMes, out var valorGDIMes, out var qtdSCMes, out var valorSCMes);

            decimal fobBH = db.g_produtos.AsNoTracking()
                .Where(p => p.saldo_01_disponivel > 0)
                .Select(p => (decimal?)(p.saldo_01_disponivel * p.fob1_dollar))
                .Sum() ?? 0m;
            decimal fobSP = db.g_produtos.AsNoTracking()
                .Where(p => p.saldo_03_disponivel > 0)
                .Select(p => (decimal?)(p.saldo_03_disponivel * p.fob1_dollar))
                .Sum() ?? 0m;

            var sb = new StringBuilder();
            sb.Append("✈️ GDI - Relatório Gerencial\n");
            sb.Append("📅 " + dataHora.ToString("dd/MM/yyyy") + " | " + dataHora.ToString("HH:mm") + "\n");
            sb.Append("\n");
            AppendSecaoPedidos(sb, "📦 Pedidos Hoje",
                qtdDiarioPorVendedor, valorDiarioPorVendedor,
                qtdGDIHoje, valorGDIHoje, qtdSCHoje, valorSCHoje);
            AppendSecaoPedidos(sb, "📊 Pedidos Mês",
                qtdMesPorVendedor, valorMesPorVendedor,
                qtdGDIMes, valorGDIMes, qtdSCMes, valorSCMes);
            sb.Append("📦 Estoque (Fob)\n");
            sb.Append("BH    US$ " + fobBH.ToString("N2", PtBR) + "\n");
            sb.Append("SP    US$ " + fobSP.ToString("N2", PtBR) + "\n");
            sb.Append("Total US$ " + (fobBH + fobSP).ToString("N2", PtBR) + "\n");

            return sb.ToString().TrimEnd('\n');
        }

        private static void AgregarPedidosPorVendedor(
            DataTable table,
            out Dictionary<int, int> qtdPorVendedor,
            out Dictionary<int, decimal> valorPorVendedor,
            out int qtdGDI,
            out decimal valorGDI,
            out int qtdSC,
            out decimal valorSC)
        {
            qtdPorVendedor = new Dictionary<int, int>();
            valorPorVendedor = new Dictionary<int, decimal>();
            qtdGDI = 0;
            valorGDI = 0m;
            qtdSC = 0;
            valorSC = 0m;

            foreach (DataRow row in table.AsEnumerable())
            {
                decimal.TryParse(row["valor"].EmptyIfNull().ToString(), out decimal valor);
                int.TryParse(row["qtd"].EmptyIfNull().ToString(), out int qtd);
                int.TryParse(row["id_vendedor"].EmptyIfNull().ToString(), out int idVendedor);

                qtdPorVendedor[idVendedor] = qtd;
                valorPorVendedor[idVendedor] = valor;

                if (idVendedor == 6)
                {
                    qtdSC += qtd;
                    valorSC += valor;
                }
                else
                {
                    qtdGDI += qtd;
                    valorGDI += valor;
                }
            }
        }

        private static void AppendSecaoPedidos(
            StringBuilder sb,
            string titulo,
            Dictionary<int, int> qtdPorVendedor,
            Dictionary<int, decimal> valorPorVendedor,
            int qtdGDI,
            decimal valorGDI,
            int qtdSC,
            decimal valorSC)
        {
            sb.Append(titulo + "\n");
            foreach (int idVendedor in IdsVendedoresOrdemAlfabetica)
            {
                int qtd = qtdPorVendedor.ContainsKey(idVendedor) ? qtdPorVendedor[idVendedor] : 0;
                decimal valor = valorPorVendedor.ContainsKey(idVendedor) ? valorPorVendedor[idVendedor] : 0m;
                sb.Append(NomesVendedor[idVendedor] + " " + qtd + " | R$ " + valor.ToString("N2", PtBR) + "\n");
            }
            sb.Append("**********\n");
            sb.Append("GDI   " + qtdGDI + " | R$ " + valorGDI.ToString("N2", PtBR) + "\n");
            sb.Append("SC    " + qtdSC + " | R$ " + valorSC.ToString("N2", PtBR) + "\n");
            sb.Append("Total " + (qtdGDI + qtdSC) + " | R$ " + (valorGDI + valorSC).ToString("N2", PtBR) + "\n");
            sb.Append("\n");
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
        }
    }
}
