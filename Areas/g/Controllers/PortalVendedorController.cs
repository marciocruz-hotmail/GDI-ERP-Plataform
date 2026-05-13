using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Portal financeiro do vendedor — títulos (g_financeiro) dos clientes da carteira.</summary>
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PortalVendedor_Default,g_PortalVendedor_*")]
    public class PortalVendedorController : Controller
    {
        private GdiPlataformEntities db;

        public PortalVendedorController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            return RedirectToAction("PortalFinanceiro");
        }

        public ActionResult PortalFinanceiro()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
                if (db == null)
                {
                    return JsonDataTableException(new InvalidOperationException("Banco de dados não inicializado."), param, filterOnOff);
                }

                int? idVendedorFiltro = null;
                if (CachePersister.userIdentity != null && CachePersister.userIdentity.IdPerfil == -800)
                {
                    idVendedorFiltro = CachePersister.userIdentity.IdVendedor;
                }

                var query =
                    from f in db.g_financeiro.AsNoTracking()
                    join c in db.g_clientes.AsNoTracking() on f.id_cliente equals c.id_cliente
                    where f.id_financeiro > 0
                    select new { Financeiro = f, Cliente = c };

                if (idVendedorFiltro.HasValue && idVendedorFiltro.Value > 0)
                {
                    int idV = idVendedorFiltro.Value;
                    query = query.Where(x => x.Cliente.id_vendedor == idV || x.Cliente.id_vendedor2 == idV || x.Cliente.id_vendedor3 == idV || x.Cliente.id_vendedor4 == idV);
                }

                if (!param.yesFilterField.EmptyIfNull().ToString().Trim().Equals(String.Empty)
                    && !param.yesFilterField.EmptyIfNull().ToString().Trim().Equals("*"))
                {
                    filterOnOff = "1";
                    string sql = "select f.* from g_financeiro f inner join g_clientes c on f.id_cliente = c.id_cliente where f.id_financeiro > 0 and ";
                    sql += LibStringFormat.SentencaSQLFiltroGenerico(param.yesFilterField, param.yesFilterOperador, param.yesFilterText);
                    if (idVendedorFiltro.HasValue && idVendedorFiltro.Value > 0)
                    {
                        int idV = idVendedorFiltro.Value;
                        sql += " and (c.id_vendedor = " + idV + " or c.id_vendedor2 = " + idV + " or c.id_vendedor3 = " + idV + " or c.id_vendedor4 = " + idV + ")";
                    }
                    LibDB.setFilterByUser(sql, "g_Financeiro", true, db);
                    var ids = db.g_financeiro.SqlQuery(sql).Select(f => f.id_financeiro).ToList();
                    var idSet = new HashSet<int>(ids);
                    query = query.Where(x => idSet.Contains(x.Financeiro.id_financeiro));
                }

                var allList = query.OrderBy(x => x.Financeiro.data_vencimento).ToList();
                int total = allList.Count;
                var page = allList.Skip(param.iDisplayStart).Take(param.iDisplayLength <= 0 ? 20 : param.iDisplayLength).ToList();

                var ci = CultureInfo.GetCultureInfo("pt-BR");
                var list = new List<string[]>();
                foreach (var x in page)
                {
                    string nomeCliente = x.Cliente != null ? x.Cliente.nome.EmptyIfNull().ToString() : String.Empty;
                    nomeCliente = nomeCliente.Replace(",", " ");
                    list.Add(new[]
                    {
                        "",
                        x.Financeiro.id_financeiro.ToString(),
                        nomeCliente,
                        x.Financeiro.data_vencimento.ToString("dd/MM/yy", ci),
                        string.Format(ci, "{0:C}", x.Financeiro.valor_total_liquido).Replace("R$ ", "").Replace("R$", "").Replace("$", "")
                    });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = total,
                    iTotalDisplayRecords = total,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = yesFilterOnOff ?? "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }

        [CustomAuthorize(Roles = "*")]
        protected override void Dispose(bool disposing)
        {
            if (disposing && db != null) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
