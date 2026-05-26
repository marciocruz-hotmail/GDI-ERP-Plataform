using System;
using System.Web.Mvc;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Typeahead Ajax produtos — NF entrada nacional (processamento por linha).</summary>
    public partial class MovimentosEntradasController
    {
        /// <summary>GET — contrato { items: [{ id, text }], errorMessage?, severity? }. Parâmetros: q (mín. 2), id, limit.</summary>
        [HttpGet]
        public JsonResult GetProdutosLookup(string q, int? id, int? limit)
        {
            try
            {
                if (db == null)
                    return JsonLookupError("Sessão de banco indisponível.");
                var items = LookupSearchQueries.SearchProdutos(db, q, id, limit ?? 0);
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
