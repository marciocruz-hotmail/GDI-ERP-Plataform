using System;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Controllers
{
    /// <summary>Typeahead Ajax clientes/fornecedores — CACHE-2b/2c (sem combo global em HTML).</summary>
    public class ClientesLookupController : Controller
    {
        private GdiPlataformEntities Db
        {
            get
            {
                if (CachePersister.dataBase.EmptyIfNull().ToString().Equals(string.Empty))
                    return null;
                return new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        private const string RolesLookup =
            "SuperAdmin,Admin," +
            "g_Financeiro_*,g_Financeiro_Default,g_Clientes_*,g_Clientes_Default,g_ContratosAviacao_*,g_ContratosAviacao_Default," +
            "gc_FinanceiroLancamentos_*,gc_Movimentos_*,gc_RelatoriosFinanceiros_*";

        [HttpGet]
        [CustomAuthorize(Roles = RolesLookup)]
        public JsonResult GetClientesFornecedoresLookup(string q, int? id, int? limit)
        {
            return JsonFornecedores(q, id, limit, comDoc: false);
        }

        [HttpGet]
        [CustomAuthorize(Roles = RolesLookup)]
        public JsonResult GetClientesFornecedoresComDocLookup(string q, int? id, int? limit)
        {
            return JsonFornecedores(q, id, limit, comDoc: true);
        }

        private JsonResult JsonFornecedores(string q, int? id, int? limit, bool comDoc)
        {
            try
            {
                var db = Db;
                if (db == null)
                    return JsonLookupError("Sessão de banco indisponível.");
                var items = LookupSearchQueries.SearchClientesFornecedores(db, q, id, limit ?? 0, comDoc);
                return Json(new LookupAjaxItemsResponse { items = items }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return JsonLookupError(LibExceptions.getExceptionShortMessage(ex));
            }
        }

        private static JsonResult JsonLookupError(string message)
        {
            return new JsonResult
            {
                Data = new LookupAjaxItemsResponse
                {
                    items = new System.Collections.Generic.List<LookupAjaxItemDto>(),
                    errorMessage = message,
                    severity = "error"
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
    }
}
