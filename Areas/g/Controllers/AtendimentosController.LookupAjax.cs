using System;
using System.Web.Mvc;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Typeahead Ajax clientes — atendimentos (CACHE-2a).</summary>
    public partial class AtendimentosController
    {
        private const string RolesAtendimentoLookup = "SuperAdmin,Admin,g_Atendimentos_*,g_Atendimentos_Default";

        /// <summary>GET — contrato { items: [{ id, text }], errorMessage?, severity? }.</summary>
        [HttpGet]
        [CustomAuthorize(Roles = RolesAtendimentoLookup)]
        public JsonResult GetClientesLookup(string q, int? id, int? limit)
        {
            try
            {
                if (db == null)
                    return JsonLookupError("Sessão de banco indisponível.");
                var items = LookupSearchQueries.SearchClientes(db, q, id, limit ?? 0);
                return Json(new LookupAjaxItemsResponse { items = items }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return JsonLookupError(GdiMvcJsonResults.AjaxFailureMessage(ex));
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
