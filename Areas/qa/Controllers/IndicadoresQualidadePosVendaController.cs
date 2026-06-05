using ClosedXML.Excel;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web.Mvc;

namespace GdiPlataform.Areas.qa.Controllers
{
    public class IndicadoresQualidadePosVendaController : Controller
    {
        private GdiPlataformEntities db;

        public IndicadoresQualidadePosVendaController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        // ─── Filtro helper ──────────────────────────────────────────────────────

        private static string BuildFiltroWhere(int trimestre, int ano, int idCliente, string resultado,
                                                out int mesIni, out int mesFim)
        {
            mesIni = (trimestre - 1) * 3 + 1;
            mesFim = mesIni + 2;

            string where = $@"
                WHERE id_movimento_posicao >= 6
                  AND posvenda_datahora_contato IS NOT NULL
                  AND YEAR(posvenda_datahora_contato) = {ano}
                  AND MONTH(posvenda_datahora_contato) BETWEEN {mesIni} AND {mesFim}";

            if (idCliente > 0)
                where += $" AND id_cliente = {idCliente}";

            if (resultado == "C")
                where += " AND posvenda_pedido_nao_conforme = 0";
            else if (resultado == "NC")
                where += " AND posvenda_pedido_nao_conforme = 1";

            return where;
        }

        // ─── Index ───────────────────────────────────────────────────────────────

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-star", "", "#008000", "fa-lg")
                + LibStringFormat.GetTabHtml(1) + "Qualidade - Indicadores Pós-Venda";

            var trimestres = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "T1 — Jan / Fev / Mar" },
                new SelectListItem { Value = "2", Text = "T2 — Abr / Mai / Jun" },
                new SelectListItem { Value = "3", Text = "T3 — Jul / Ago / Set" },
                new SelectListItem { Value = "4", Text = "T4 — Out / Nov / Dez" },
            };
            ViewBag.Trimestres = trimestres;

            var anos = new List<SelectListItem>();
            int anoAtual = DateTime.Now.Year;
            for (int i = anoAtual; i >= anoAtual - 2; i--)
                anos.Add(new SelectListItem { Value = i.ToString(), Text = i.ToString() });
            ViewBag.Anos = anos;

            ViewBag.TrimestreAtual = ((DateTime.Now.Month - 1) / 3 + 1).ToString();
            ViewBag.AnoAtual = anoAtual.ToString();

            return View();
        }

        // ─── KPIs (9 indicadores) ────────────────────────────────────────────────

        public ActionResult GetDadosKpi(int trimestre, int ano, int idCliente = 0, string resultado = "")
        {
            try
            {
                string where = BuildFiltroWhere(trimestre, ano, idCliente, resultado,
                                                out int mesIni, out int mesFim);

                string sqlKpi = $@"
                    SELECT
                        COUNT(*) AS total_avaliacoes,
                        AVG(CAST(posvenda_nota_avaliacao_geral                      AS float)) AS isg,
                        AVG(CAST(posvenda_nota_avaliacao_atendimento_equipe          AS float)) AS isa,
                        AVG(CAST(posvenda_nota_avaliacao_prazo_entrega               AS float)) AS isp,
                        AVG(CAST(posvenda_nota_avaliacao_clareza_informacoes_comerciais AS float)) AS isi,
                        100.0 * SUM(CASE WHEN posvenda_recebimento_conforme_combinado   = 1 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS tce,
                        100.0 * SUM(CASE WHEN posvenda_mercadoria_estado_fisico_ok      = 1 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS tefi,
                        100.0 * SUM(CASE WHEN posvenda_itens_correspondem_pedido_nf     = 1 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS tci,
                        100.0 * SUM(CASE WHEN posvenda_pedido_nao_conforme              = 1 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS tnc,
                        (
                            SELECT COUNT(*)
                            FROM gc_movimentos t2
                            WHERE t2.id_movimento_posicao >= 6
                              AND YEAR(t2.datahora_cadastro) = {ano}
                              AND MONTH(t2.datahora_cadastro) BETWEEN {mesIni} AND {mesFim}
                              {(idCliente > 0 ? $"AND t2.id_cliente = {idCliente}" : "")}
                        ) AS total_entregues
                    FROM gc_movimentos
                    {where}";

                DataTable dt = LibDB.GetDataTable(sqlKpi, db);

                double isg = 0, isa = 0, isp = 0, isi = 0;
                double tce = 0, tefi = 0, tci = 0, tnc = 0, trc = 0;
                int totalAvaliacoes = 0, totalEntregues = 0;

                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    totalAvaliacoes = r["total_avaliacoes"] != DBNull.Value ? Convert.ToInt32(r["total_avaliacoes"]) : 0;
                    totalEntregues  = r["total_entregues"]  != DBNull.Value ? Convert.ToInt32(r["total_entregues"])  : 0;
                    isg  = Val(r, "isg");
                    isa  = Val(r, "isa");
                    isp  = Val(r, "isp");
                    isi  = Val(r, "isi");
                    tce  = Val(r, "tce");
                    tefi = Val(r, "tefi");
                    tci  = Val(r, "tci");
                    tnc  = Val(r, "tnc");
                    trc  = totalEntregues > 0 ? Math.Round(100.0 * totalAvaliacoes / totalEntregues, 1) : 0;
                }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace   = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    totalAvaliacoes,
                    totalEntregues,
                    isg  = Round1(isg),  corIsg  = CorNota(isg,  4.0, 3.6),
                    isa  = Round1(isa),  corIsa  = CorNota(isa,  4.0, 3.6),
                    isp  = Round1(isp),  corIsp  = CorNota(isp,  3.8, 3.4),
                    isi  = Round1(isi),  corIsi  = CorNota(isi,  4.0, 3.6),
                    tce  = Round1(tce),  corTce  = CorPct(tce,  95, 90, false),
                    tefi = Round1(tefi), corTefi = CorPct(tefi, 98, 93, false),
                    tci  = Round1(tci),  corTci  = CorPct(tci,  98, 93, false),
                    tnc  = Round1(tnc),  corTnc  = CorPct(tnc,   3,  5, true),
                    trc  = Round1(trc),  corTrc  = CorPct(trc,  60, 45, false),
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.AjaxFailureMessage(e),
                    severity     = GdiMvcJsonResults.SeverityError,
                    stackTrace   = e.ToString()
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─── Gráfico de tendência (Flot) — últimos 8 trimestres ─────────────────

        public ActionResult GetDadosGraficoTendencia(int trimestre, int ano)
        {
            try
            {
                // Janela: 8 trimestres retroativos a partir do selecionado
                string sql = @"
                    SELECT
                        YEAR(posvenda_datahora_contato)                    AS ano,
                        DATEPART(QUARTER, posvenda_datahora_contato)       AS trimestre,
                        AVG(CAST(posvenda_nota_avaliacao_geral AS float))   AS isg,
                        100.0 * SUM(CASE WHEN posvenda_pedido_nao_conforme = 1 THEN 1 ELSE 0 END)
                            / NULLIF(COUNT(*), 0)                           AS tnc,
                        COUNT(*)                                            AS total
                    FROM gc_movimentos
                    WHERE id_movimento_posicao >= 6
                      AND posvenda_datahora_contato IS NOT NULL
                      AND posvenda_datahora_contato >= DATEADD(MONTH, -24,
                            DATEFROMPARTS(" + ano + ", ((" + trimestre + @" - 1) * 3 + 1), 1))
                      AND posvenda_datahora_contato < DATEADD(MONTH, 3,
                            DATEFROMPARTS(" + ano + ", ((" + trimestre + @" - 1) * 3 + 1), 1))
                               + CASE WHEN " + trimestre + @" = 4 THEN 0 ELSE 0 END
                    GROUP BY YEAR(posvenda_datahora_contato), DATEPART(QUARTER, posvenda_datahora_contato)
                    ORDER BY YEAR(posvenda_datahora_contato), DATEPART(QUARTER, posvenda_datahora_contato)";

                // Versão simplificada: últimas 8 trimestres com base nos dados existentes
                string sqlSimples = @"
                    SELECT TOP 8
                        YEAR(posvenda_datahora_contato)                     AS ano,
                        DATEPART(QUARTER, posvenda_datahora_contato)        AS trim,
                        AVG(CAST(posvenda_nota_avaliacao_geral AS float))    AS isg,
                        100.0 * SUM(CASE WHEN posvenda_pedido_nao_conforme = 1 THEN 1 ELSE 0 END)
                            / NULLIF(COUNT(*), 0)                            AS tnc
                    FROM gc_movimentos
                    WHERE id_movimento_posicao >= 6
                      AND posvenda_datahora_contato IS NOT NULL
                    GROUP BY YEAR(posvenda_datahora_contato), DATEPART(QUARTER, posvenda_datahora_contato)
                    ORDER BY YEAR(posvenda_datahora_contato) DESC, DATEPART(QUARTER, posvenda_datahora_contato) DESC";

                DataTable dt = LibDB.GetDataTable(sqlSimples, db);

                var listIsg   = new List<string[]>();
                var listTnc   = new List<string[]>();
                var listTicks = new List<string[]>();
                int idx = 0;

                // Inverter para ordem cronológica
                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    idx++;
                    DataRow r = dt.Rows[i];
                    string label = "T" + r["trim"] + "/" + r["ano"].ToString().Substring(2);
                    listTicks.Add(new[] { idx.ToString(), label });
                    listIsg.Add(new[] { idx.ToString(), Round1(r["isg"] != DBNull.Value ? Convert.ToDouble(r["isg"]) : 0).ToString(System.Globalization.CultureInfo.InvariantCulture) });
                    listTnc.Add(new[] { idx.ToString(), Round1(r["tnc"] != DBNull.Value ? Convert.ToDouble(r["tnc"]) : 0).ToString(System.Globalization.CultureInfo.InvariantCulture) });
                }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace   = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    dataIsg   = listIsg,
                    dataTnc   = listTnc,
                    dataTicks = listTicks
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.AjaxFailureMessage(e),
                    severity     = GdiMvcJsonResults.SeverityError,
                    stackTrace   = e.ToString(),
                    dataIsg      = new List<string[]>(),
                    dataTnc      = new List<string[]>(),
                    dataTicks    = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─── DataTables — listagem detalhada ────────────────────────────────────

        public ActionResult GetDadosRelatorio(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
                int trimestre = 0, ano = 0, idCliente = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull(), out trimestre);
                int.TryParse(param.yesCustomField02.EmptyIfNull(), out ano);
                int.TryParse(param.yesCustomField03.EmptyIfNull(), out idCliente);
                string resultado = param.yesCustomField04.EmptyIfNull();

                if (trimestre == 0) trimestre = (DateTime.Now.Month - 1) / 3 + 1;
                if (ano == 0) ano = DateTime.Now.Year;

                string where = BuildFiltroWhere(trimestre, ano, idCliente, resultado,
                                                out int mesIni, out int mesFim);
                if (idCliente > 0 || !string.IsNullOrWhiteSpace(resultado)) filterOnOff = "1";

                int start  = param.iDisplayStart;
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;

                string sqlCount = $"SELECT COUNT(*) FROM gc_movimentos {where}";
                string sqlData  = $@"
                    SELECT
                        m.id_movimento,
                        m.oc_numero,
                        ISNULL(NULLIF(RTRIM(c.nome_fantasia), ''), ISNULL(c.razao_social, '')) AS nome_cliente,
                        CONVERT(varchar(10), m.posvenda_datahora_contato, 103)                 AS data_contato,
                        ISNULL(CAST(m.posvenda_nota_avaliacao_geral AS varchar), '-')                          AS nota_geral,
                        ISNULL(CAST(m.posvenda_nota_avaliacao_prazo_entrega AS varchar), '-')                  AS nota_prazo,
                        ISNULL(CAST(m.posvenda_nota_avaliacao_atendimento_equipe AS varchar), '-')             AS nota_atend,
                        ISNULL(CAST(m.posvenda_nota_avaliacao_clareza_informacoes_comerciais AS varchar), '-') AS nota_info,
                        CASE WHEN m.posvenda_recebimento_conforme_combinado = 1 THEN 'Sim' ELSE 'Não' END     AS conforme,
                        CASE WHEN m.posvenda_pedido_nao_conforme = 1 THEN 'Sim' ELSE 'Não' END                AS nao_conforme,
                        CASE WHEN m.posvenda_cliente_sugeriu_melhoria = 1 THEN 'Sim' ELSE 'Não' END           AS sugestao
                    FROM gc_movimentos m
                    LEFT JOIN g_clientes c ON c.id_cliente = m.id_cliente AND m.id_cliente > 0
                    {where}
                    ORDER BY m.posvenda_datahora_contato DESC
                    OFFSET {start} ROWS FETCH NEXT {length} ROWS ONLY";

                int total = Convert.ToInt32(LibDB.GetDataTable(sqlCount, db).Rows[0][0]);
                DataTable dt = LibDB.GetDataTable(sqlData, db);

                var aaData = new List<string[]>();
                foreach (DataRow r in dt.Rows)
                {
                    string corNc = r["nao_conforme"].ToString() == "Sim"
                        ? "<span class='badge bg-danger'>Sim</span>"
                        : "<span class='badge bg-success'>Não</span>";
                    string corSug = r["sugestao"].ToString() == "Sim"
                        ? "<span class='badge bg-info'>Sim</span>"
                        : "<span class='badge bg-light text-dark'>Não</span>";

                    aaData.Add(new[]
                    {
                        r["id_movimento"].ToString(),
                        r["oc_numero"].ToString(),
                        r["nome_cliente"].ToString(),
                        r["data_contato"].ToString(),
                        r["nota_geral"].ToString(),
                        r["nota_prazo"].ToString(),
                        r["nota_atend"].ToString(),
                        r["nota_info"].ToString(),
                        r["conforme"].ToString(),
                        corNc,
                        corSug
                    });
                }

                return Json(new
                {
                    errorMessage         = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace           = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff       = filterOnOff,
                    sEcho                = param.sEcho,
                    iTotalRecords        = total,
                    iTotalDisplayRecords = total,
                    aaData
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        // ─── Export Excel ────────────────────────────────────────────────────────

        public ActionResult ExportarExcel(int trimestre, int ano, int idCliente = 0, string resultado = "")
        {
            try
            {
                string where = BuildFiltroWhere(trimestre, ano, idCliente, resultado,
                                                out int mesIni, out int mesFim);

                // KPIs
                string sqlKpi = $@"
                    SELECT
                        COUNT(*) AS total_avaliacoes,
                        AVG(CAST(posvenda_nota_avaliacao_geral AS float))                          AS isg,
                        AVG(CAST(posvenda_nota_avaliacao_atendimento_equipe AS float))              AS isa,
                        AVG(CAST(posvenda_nota_avaliacao_prazo_entrega AS float))                   AS isp,
                        AVG(CAST(posvenda_nota_avaliacao_clareza_informacoes_comerciais AS float))  AS isi,
                        100.0*SUM(CASE WHEN posvenda_recebimento_conforme_combinado=1 THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) AS tce,
                        100.0*SUM(CASE WHEN posvenda_mercadoria_estado_fisico_ok=1     THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) AS tefi,
                        100.0*SUM(CASE WHEN posvenda_itens_correspondem_pedido_nf=1    THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) AS tci,
                        100.0*SUM(CASE WHEN posvenda_pedido_nao_conforme=1             THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) AS tnc,
                        (SELECT COUNT(*) FROM gc_movimentos t2
                         WHERE t2.id_movimento_posicao >= 6
                           AND YEAR(t2.datahora_cadastro) = {ano}
                           AND MONTH(t2.datahora_cadastro) BETWEEN {mesIni} AND {mesFim}
                           {(idCliente > 0 ? $"AND t2.id_cliente = {idCliente}" : "")}) AS total_entregues
                    FROM gc_movimentos {where}";

                string sqlDet = $@"
                    SELECT
                        m.id_movimento                                                                         AS [Pedido],
                        m.oc_numero                                                                            AS [OC Cliente],
                        ISNULL(NULLIF(RTRIM(c.nome_fantasia), ''), ISNULL(c.razao_social, ''))                 AS [Cliente],
                        CONVERT(varchar(10), m.posvenda_datahora_contato, 103)                                 AS [Data Contato],
                        m.posvenda_nota_avaliacao_geral                                                        AS [Nota Geral],
                        m.posvenda_nota_avaliacao_prazo_entrega                                                AS [Nota Prazo],
                        m.posvenda_nota_avaliacao_atendimento_equipe                                           AS [Nota Atend.],
                        m.posvenda_nota_avaliacao_clareza_informacoes_comerciais                               AS [Nota Info.],
                        CASE WHEN m.posvenda_recebimento_conforme_combinado=1 THEN 'Sim' ELSE 'Não' END        AS [Conforme?],
                        CASE WHEN m.posvenda_pedido_nao_conforme=1 THEN 'Sim' ELSE 'Não' END                   AS [NC?],
                        CASE WHEN m.posvenda_cliente_sugeriu_melhoria=1 THEN 'Sim' ELSE 'Não' END              AS [Sugestão?],
                        m.posvenda_descricao_nao_conformidade                                                  AS [Descrição NC],
                        m.posvenda_descricao_sugestao_melhoria                                                 AS [Sugestão Melhoria],
                        m.posvenda_observacao_recebimento_pedido                                               AS [Obs. Cliente]
                    FROM gc_movimentos m
                    LEFT JOIN g_clientes c ON c.id_cliente = m.id_cliente AND m.id_cliente > 0
                    {where}
                    ORDER BY m.posvenda_datahora_contato DESC";

                DataTable dtKpi = LibDB.GetDataTable(sqlKpi, db);
                DataTable dtDet = LibDB.GetDataTable(sqlDet, db);

                using (var wb = new XLWorkbook())
                {
                    // Aba 1 — Resumo KPIs
                    var wsKpi = wb.Worksheets.Add("Resumo KPIs");
                    wsKpi.Cell(1, 1).Value = "Indicador";
                    wsKpi.Cell(1, 2).Value = "Valor";
                    wsKpi.Cell(1, 3).Value = "Meta";
                    wsKpi.Row(1).Style.Font.Bold = true;

                    var linhasKpi = new[]
                    {
                        new[] { "ISG — Satisfação Geral (média 1-5)",            "isg",  "≥ 4,0" },
                        new[] { "ISA — Satisfação Atendimento (média 1-5)",       "isa",  "≥ 4,0" },
                        new[] { "ISP — Satisfação Prazo Entrega (média 1-5)",     "isp",  "≥ 3,8" },
                        new[] { "ISI — Satisfação Clareza Informações (média 1-5)","isi", "≥ 4,0" },
                        new[] { "TCE — Taxa Conformidade Entrega (%)",            "tce",  "≥ 95%" },
                        new[] { "TEFI — Taxa Integridade Física (%)",             "tefi", "≥ 98%" },
                        new[] { "TCI — Taxa Conformidade Itens (%)",              "tci",  "≥ 98%" },
                        new[] { "TNC — Taxa Não Conformidade (%)",                "tnc",  "≤ 3%"  },
                        new[] { "TRC — Taxa de Resposta (%)",                     "trc",  "≥ 60%" },
                    };

                    DataRow kRow = dtKpi.Rows.Count > 0 ? dtKpi.Rows[0] : null;
                    int totalAv = kRow != null && kRow["total_avaliacoes"] != DBNull.Value ? Convert.ToInt32(kRow["total_avaliacoes"]) : 0;
                    int totalEn = kRow != null && kRow["total_entregues"]  != DBNull.Value ? Convert.ToInt32(kRow["total_entregues"])  : 0;
                    double trcVal = totalEn > 0 ? Math.Round(100.0 * totalAv / totalEn, 1) : 0;

                    for (int i = 0; i < linhasKpi.Length; i++)
                    {
                        double val = 0;
                        string colName = linhasKpi[i][1];
                        if (colName == "trc") { val = trcVal; }
                        else if (kRow != null && kRow[colName] != DBNull.Value) val = Math.Round(Convert.ToDouble(kRow[colName]), 1);

                        wsKpi.Cell(i + 2, 1).Value = linhasKpi[i][0];
                        wsKpi.Cell(i + 2, 2).Value = val;
                        wsKpi.Cell(i + 2, 3).Value = linhasKpi[i][2];
                    }

                    wsKpi.Cell(linhasKpi.Length + 3, 1).Value = "Total de avaliações no período";
                    wsKpi.Cell(linhasKpi.Length + 3, 2).Value = totalAv;
                    wsKpi.Cell(linhasKpi.Length + 4, 1).Value = "Total de pedidos entregues no período";
                    wsKpi.Cell(linhasKpi.Length + 4, 2).Value = totalEn;
                    wsKpi.Columns().AdjustToContents();

                    // Aba 2 — Avaliações Detalhadas
                    var wsDet = wb.Worksheets.Add("Avaliacoes");
                    if (dtDet.Columns.Count > 0)
                    {
                        for (int c = 0; c < dtDet.Columns.Count; c++)
                            wsDet.Cell(1, c + 1).Value = dtDet.Columns[c].ColumnName;
                        wsDet.Row(1).Style.Font.Bold = true;

                        for (int row = 0; row < dtDet.Rows.Count; row++)
                            for (int col = 0; col < dtDet.Columns.Count; col++)
                            {
                                object v = dtDet.Rows[row][col];
                                if (v == DBNull.Value) wsDet.Cell(row + 2, col + 1).Value = "";
                                else wsDet.Cell(row + 2, col + 1).Value = v.ToString();
                            }
                        wsDet.Columns().AdjustToContents();
                    }

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        string nomeArquivo = $"IndicadoresPosVenda_T{trimestre}_{ano}.xlsx";
                        return File(ms.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            nomeArquivo);
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.AjaxFailureMessage(e),
                    severity     = GdiMvcJsonResults.SeverityError,
                    stackTrace   = e.ToString()
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─── Helpers privados ────────────────────────────────────────────────────

        private static double Val(DataRow r, string col)
            => r[col] != DBNull.Value ? Math.Round(Convert.ToDouble(r[col]), 1) : 0;

        private static double Round1(object v)
            => v is double d ? Math.Round(d, 1) : Math.Round(Convert.ToDouble(v), 1);

        private static string CorNota(double val, double metaBoa, double metaMedia)
            => val >= metaBoa ? "success" : val >= metaMedia ? "warning" : "danger";

        // invertido=true: menor é melhor (TNC)
        private static string CorPct(double val, double metaBoa, double metaMedia, bool invertido)
        {
            if (!invertido) return val >= metaBoa ? "success" : val >= metaMedia ? "warning" : "danger";
            return val <= metaBoa ? "success" : val <= metaMedia ? "warning" : "danger";
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string filterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, filterOnOff), JsonRequestBehavior.AllowGet);
        }
    }
}
