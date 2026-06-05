using System;
using System.Web.Mvc;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Typeahead Ajax clientes/produtos — formulário de pedido (1.6).</summary>
    public partial class MovimentosController
    {
        private const string RolesPedidoLookup = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_IndexPedido";

        /// <summary>GET — contrato { items: [{ id, text }], errorMessage?, severity? }. Parâmetros: q (mín. 2), id (pré-seleção), limit (opc., máx. 50).</summary>
        [HttpGet]
        [CustomAuthorize(Roles = RolesPedidoLookup)]
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

        /// <summary>GET — contrato { items: [{ id, text }] }. Parâmetros: q, id, limit.</summary>
        [HttpGet]
        [CustomAuthorize(Roles = RolesPedidoLookup)]
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
