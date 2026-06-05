using GdiPlataform.Controllers;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Net;

namespace GdiPlataform.Lib
{
    /// <summary>
    /// Contrato JSON centralizado para respostas MVC (DataTables server-side e Ajax POST).
    /// Compatível com <c>GdiDtNotifyJsonErrorMessage</c> / <c>GdiAjaxNotifyInconsistencias</c> (start.js).
    /// Substituir gradualmente helpers privados <c>JsonDataTableException</c> / <c>JsonAjaxErro*</c> nos controllers.
    /// </summary>
    public static class GdiMvcJsonResults
    {
        public const string SeverityError = "error";

        #region DataTables (Fase 8+)

        /// <summary>
        /// JSON de erro DataTables — propriedades obrigatórias para o cliente <c>xhr.dt</c>.
        /// </summary>
        public static object DataTableError(
            Exception e,
            jQueryDataTableParamModel param,
            string yesFilterOnOff = "0",
            string severity = null)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            return new
            {
                errorMessage = LibExceptions.getExceptionShortMessage(e),
                severity = severity ?? SeverityError,
                stackTrace = e.ToString(),
                yesFilterOnOff = yesFilterOnOff ?? "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            };
        }

        /// <summary>
        /// Campos de integridade vazios no JSON de sucesso (evita ramos especiais no cliente).
        /// Incluir no <c>return Json(new { ... })</c> junto com sEcho, aaData, totais, yesFilterOnOff.
        /// </summary>
        public static string DataTableSuccessErrorMessage => string.Empty;

        /// <summary>Par com <see cref="DataTableSuccessErrorMessage"/> no sucesso DataTables.</summary>
        public static string DataTableSuccessStackTrace => string.Empty;

        #endregion

        #region Ajax POST ({ success, msg })

        public static object AjaxFailure(string msg)
        {
            return new { success = false, msg = msg ?? string.Empty };
        }

        public static object AjaxFailure(Exception ex)
        {
            return new { success = false, msg = LibExceptions.getExceptionShortMessage(ex) };
        }

        public static object AjaxFailureValidation(DbEntityValidationException ex)
        {
            return new { success = false, msg = LibExceptions.getDbEntityValidationException(ex) };
        }

        /// <summary>Texto de falha Ajax para helpers que devolvem bool/string (sem JsonResult).</summary>
        public static string AjaxFailureMessage(Exception ex)
        {
            return LibExceptions.getExceptionShortMessage(ex);
        }

        /// <summary>Texto de validação EF para helpers que devolvem bool/string (sem JsonResult).</summary>
        public static string AjaxFailureValidationMessage(DbEntityValidationException ex)
        {
            return LibExceptions.getDbEntityValidationException(ex);
        }

        /// <summary>Texto Ajax para <see cref="WebException"/> (robô/API) — preserva leitura do response body.</summary>
        public static string AjaxFailureWebMessage(WebException ex)
        {
            return LibExceptions.getWebException(ex);
        }

        /// <summary>Ajax POST com <c>idProcessamento</c> (exports/lotes) — objeto pronto para <c>return Json(...)</c>.</summary>
        public static object AjaxFailureIdProcessamento(Exception ex, string idProcessamento)
        {
            return new
            {
                success = false,
                msg = AjaxFailureMessage(ex),
                idProcessamento = idProcessamento ?? "0"
            };
        }

        /// <summary>Combo/typeahead com lista vazia em falha (ex.: AjaxComboComexImportacoesPedido).</summary>
        public static object AjaxFailureWithItems(Exception ex)
        {
            return new
            {
                success = false,
                msg = LibExceptions.getExceptionShortMessage(ex),
                items = new object[0]
            };
        }

        /// <summary>Texto para ViewBag.MsgBloqueio / Ajax — pedido gc.</summary>
        public static string PedidoNaoEncontradoMensagem(int idMovimento = 0)
        {
            if (idMovimento > 0)
            {
                return "Pedido Nº " + idMovimento.ToString() + " não localizado.";
            }

            return "Pedido não localizado.";
        }

        /// <summary>Pedido/movimento gc não localizado — padrão workflow Movimentos.</summary>
        public static object PedidoNaoEncontrado(int idMovimento = 0)
        {
            return AjaxFailure(PedidoNaoEncontradoMensagem(idMovimento));
        }

        /// <summary>Texto para ViewBag.MsgBloqueio / Ajax — entidade genérica.</summary>
        public static string EntidadeNaoEncontradaMensagem(string rotulo, int? id = null)
        {
            string label = (rotulo ?? "Registro").Trim();
            if (id.HasValue && id.Value > 0)
            {
                return label + " Nº " + id.Value.ToString() + " não localizado.";
            }

            return label + " não localizado.";
        }

        /// <summary>Entidade genérica não localizada (modais/Ajax fora de pedidos).</summary>
        public static object EntidadeNaoEncontrada(string rotulo, int? id = null)
        {
            return AjaxFailure(EntidadeNaoEncontradaMensagem(rotulo, id));
        }

        #endregion
    }
}
