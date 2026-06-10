using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class FretesController
    {
        #region GetDadosFretes
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Fretes_*,gc_Fretes_Actionread")]
        public ActionResult GetDadosFretes(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
                if (db == null)
                {
                    return JsonDataTableExceptionFretes(new InvalidOperationException("Sessão de banco de dados indisponível."), param, filterOnOff);
                }

                bool pesquisarExplicito = param.yesFilterField.EmptyIfNull().ToString().Trim() == "*";
                if (!pesquisarExplicito)
                {
                    return Json(new
                    {
                        errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                        stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                        yesFilterOnOff = "0",
                        sEcho = param.sEcho,
                        iTotalRecords = 0,
                        iTotalDisplayRecords = 0,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                DateTime dataIni, dataFim;
                DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out dataIni);
                DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out dataFim);
                string termo = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string transportadoraStr = param.yesCustomField04.EmptyIfNull().ToString().Trim();
                string valorStr = param.yesCustomField06.EmptyIfNull().ToString().Trim();

                var movimentos = db.gc_movimentos.AsNoTracking()
                    .Where(m => !m.movimento_cancelado)
                    .Where(m => !m.movimento_devolvido)
                    .Where(m => m.id_movimento_tipo == 3 || m.id_movimento_tipo == 4 || m.id_movimento_tipo == 8 || m.id_movimento_tipo == 19);

                filterOnOff = "1";

                if (LibStringFormat.TryParseTermoBuscaMovimentoIdOuNf(termo, out int idMov, out string padraoNf))
                {
                    movimentos = movimentos.Where(m =>
                        (idMov > 0 && m.id_movimento == idMov) ||
                        (padraoNf != null && db.gc_movimentos_nf.Any(nf =>
                            nf.id_movimento == m.id_movimento && nf.nf_numero != null && DbFunctions.Like(nf.nf_numero, padraoNf))));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(param.yesCustomField02) && !string.IsNullOrWhiteSpace(param.yesCustomField03))
                    {
                        var dtIni = dataIni.Date;
                        var dtFim = dataFim.Date.AddDays(1).AddTicks(-1);
                        movimentos = movimentos.Where(m =>
                            (m.datahora_expedicao != null && m.datahora_expedicao >= dtIni && m.datahora_expedicao <= dtFim) ||
                            (m.datahora_entrega != null && m.datahora_entrega >= dtIni && m.datahora_entrega <= dtFim) ||
                            (m.datahora_cadastro >= dtIni && m.datahora_cadastro <= dtFim));
                    }

                    if (int.TryParse(transportadoraStr, out int idTransportadora) && idTransportadora > 0)
                    {
                        movimentos = movimentos.Where(m =>
                            m.frete1_transportadora == idTransportadora || m.frete2_transportadora == idTransportadora);
                    }

                    if (!string.IsNullOrWhiteSpace(valorStr))
                    {
                        if (decimal.TryParse(valorStr, NumberStyles.Any, new CultureInfo("pt-BR"), out decimal valor) ||
                            decimal.TryParse(valorStr, NumberStyles.Any, CultureInfo.InvariantCulture, out valor))
                        {
                            movimentos = movimentos.Where(m =>
                                m.frete_valor == valor ||
                                m.frete_gerencial == valor ||
                                m.frete1_custo == valor ||
                                (m.frete_valor + m.frete_gerencial) == valor);
                        }
                    }
                }

                int totalRecords = movimentos.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                var page = movimentos
                    .OrderByDescending(m => m.id_movimento)
                    .Skip(start)
                    .Take(length)
                    .Select(m => new
                    {
                        m.id_movimento,
                        m.id_cliente,
                        m.frete1_transportadora,
                        m.frete1_rastreio,
                        m.datahora_expedicao,
                        m.datahora_entrega,
                        m.datahora_cadastro
                    })
                    .ToList();

                var idsMov = page.Select(x => x.id_movimento).ToList();
                var idsClientes = page.Select(x => x.id_cliente).Distinct().ToList();
                var idsTransportadoras = page.Select(x => x.frete1_transportadora).Where(id => id > 0).Distinct().ToList();

                var nfPorMovimento = db.gc_movimentos_nf.AsNoTracking()
                    .Where(nf => idsMov.Contains(nf.id_movimento))
                    .Select(nf => new { nf.id_movimento, nf.nf_numero })
                    .ToList()
                    .GroupBy(nf => nf.id_movimento)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.nf_numero).FirstOrDefault());

                var clientes = db.g_clientes.AsNoTracking()
                    .Where(c => idsClientes.Contains(c.id_cliente))
                    .Select(c => new { c.id_cliente, c.nome })
                    .ToList()
                    .ToDictionary(x => x.id_cliente, x => x.nome);

                var transportadoras = idsTransportadoras.Count == 0
                    ? new Dictionary<int, string>()
                    : db.g_clientes.AsNoTracking()
                        .Where(c => idsTransportadoras.Contains(c.id_cliente))
                        .Select(c => new { c.id_cliente, c.nome })
                        .ToList()
                        .ToDictionary(x => x.id_cliente, x => x.nome);

                var list = page.Select(m =>
                {
                    string nfNumero = nfPorMovimento.TryGetValue(m.id_movimento, out var nf) ? (nf ?? "") : "";
                    string nomeCliente = clientes.TryGetValue(m.id_cliente, out var cn) ? cn : "";
                    string nomeTransportadora;
                    if (m.frete1_transportadora == 0) { nomeTransportadora = "CLIENTE RETIRA"; }
                    else if (transportadoras.TryGetValue(m.frete1_transportadora, out var tn)) { nomeTransportadora = tn; }
                    else { nomeTransportadora = ""; }

                    DateTime dataExibir = m.datahora_cadastro;
                    if (m.datahora_expedicao.HasValue) { dataExibir = m.datahora_expedicao.Value; }
                    else if (m.datahora_entrega.HasValue) { dataExibir = m.datahora_entrega.Value; }

                    return new[]
                    {
                        "",
                        m.id_movimento.ToString(),
                        nfNumero,
                        nomeCliente ?? "",
                        nomeTransportadora ?? "",
                        m.frete1_rastreio ?? "",
                        dataExibir.ToString("dd/MM/yy"),
                        ""
                    };
                }).ToList();

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableExceptionFretes(e, param, filterOnOff);
            }
        }

        private JsonResult JsonDataTableExceptionFretes(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}
