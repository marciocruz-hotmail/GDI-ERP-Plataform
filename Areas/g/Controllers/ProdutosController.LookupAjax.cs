using System;
using System.Web.Mvc;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Typeahead Ajax produtos — cadastro g/Produtos Index.</summary>
    public partial class ProdutosController
    {
        private const string RolesProdutoLookup = "SuperAdmin,Admin,g_Produtos_*,g_Produtos_Default,g_Produtos_Actionread";

        /// <summary>GET — contrato { items: [{ id, text }], errorMessage?, severity? }. Parâmetros: q (mín. 2), id, limit.</summary>
        [HttpGet]
        [CustomAuthorize(Roles = RolesProdutoLookup)]
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
