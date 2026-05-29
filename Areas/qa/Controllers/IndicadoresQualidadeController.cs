using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;

namespace GdiPlataform.Areas.qa.Controllers
{
    public class IndicadoresQualidadeController : Controller
    {
        private GdiPlataformEntities db;

        public IndicadoresQualidadeController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-chart-line", "", "#008000", "fa-lg")
                + LibStringFormat.GetTabHtml(1) + "Qualidade - Indicadores";

            // Dropdowns trimestre/ano para o filtro do painel geral
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

        // GET: resumo consolidado do grupo Pós-Venda para o card do painel geral
        public ActionResult GetKpisPosVendaResumo(int trimestre, int ano)
        {
            try
            {
                int mesIni = (trimestre - 1) * 3 + 1;
                int mesFim = mesIni + 2;

                string sql = @"
                    SELECT
                        COUNT(*) AS total_avaliacoes,
                        AVG(CAST(posvenda_nota_avaliacao_geral AS float)) AS isg,
                        100.0 * SUM(CASE WHEN posvenda_pedido_nao_conforme = 1 THEN 1 ELSE 0 END)
                            / NULLIF(COUNT(*), 0) AS tnc,
                        (
                            SELECT COUNT(*)
                            FROM gc_movimentos t
                            WHERE t.id_movimento_posicao >= 6
                              AND YEAR(t.datahora_cadastro) = " + ano + @"
                              AND MONTH(t.datahora_cadastro) BETWEEN " + mesIni + " AND " + mesFim + @"
                        ) AS total_pedidos_entregues
                    FROM gc_movimentos
                    WHERE id_movimento_posicao >= 6
                      AND posvenda_datahora_contato IS NOT NULL
                      AND YEAR(posvenda_datahora_contato) = " + ano + @"
                      AND MONTH(posvenda_datahora_contato) BETWEEN " + mesIni + " AND " + mesFim;

                DataTable dt = LibDB.GetDataTable(sql, db);

                double isg = 0, tnc = 0, trc = 0;
                int totalAvaliacoes = 0, totalEntregues = 0;

                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    totalAvaliacoes = r["total_avaliacoes"] != DBNull.Value ? Convert.ToInt32(r["total_avaliacoes"]) : 0;
                    totalEntregues  = r["total_pedidos_entregues"] != DBNull.Value ? Convert.ToInt32(r["total_pedidos_entregues"]) : 0;
                    isg = r["isg"] != DBNull.Value ? Math.Round(Convert.ToDouble(r["isg"]), 1) : 0;
                    tnc = r["tnc"] != DBNull.Value ? Math.Round(Convert.ToDouble(r["tnc"]), 1) : 0;
                    trc = totalEntregues > 0 ? Math.Round(100.0 * totalAvaliacoes / totalEntregues, 1) : 0;
                }

                // Semáforo: green / warning / danger
                string corIsg = isg >= 4.0 ? "success" : (isg >= 3.6 ? "warning" : "danger");
                string corTnc = tnc <= 3.0 ? "success" : (tnc <= 5.0 ? "warning" : "danger");
                string corTrc = trc >= 60.0 ? "success" : (trc >= 45.0 ? "warning" : "danger");

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    totalAvaliacoes,
                    totalEntregues,
                    isg,
                    tnc,
                    trc,
                    corIsg,
                    corTnc,
                    corTrc
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    errorMessage = LibExceptions.getExceptionShortMessage(e),
                    severity = "error",
                    stackTrace = e.ToString()
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
