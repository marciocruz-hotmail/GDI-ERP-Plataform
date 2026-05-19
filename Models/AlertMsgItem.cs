using System;
using System.Collections.Generic;
using System.Linq;

namespace GdiPlataform
{
    /// <summary>
    /// Um alerta Bootstrap 5 exibido por Views/Shared/_AlertMsg.cshtml (conteúdo HTML).
    /// </summary>
    public sealed class AlertMsgItem
    {
        public string Html { get; set; }
        public string AlertType { get; set; }

        /// <summary>
        /// Retorna um item se a mensagem não for vazia; caso contrário, sequência vazia.
        /// </summary>
        /// <param name="message">Texto ou HTML (será emitido com Html.Raw na partial).</param>
        /// <param name="alertType">primary, secondary, success, danger, warning, info, light, dark</param>
        /// <param name="prefixHtml">Opcional: prefixado à mensagem (ex.: rótulo em negrito).</param>
        public static IEnumerable<AlertMsgItem> Maybe(object message, string alertType = "danger", string prefixHtml = null)
        {
            var s = message as string;
            if (message != null && s == null)
                s = Convert.ToString(message);
            if (string.IsNullOrWhiteSpace(s))
                return Enumerable.Empty<AlertMsgItem>();
            var html = string.IsNullOrEmpty(prefixHtml) ? s : (prefixHtml + s);
            return new[]
            {
                new AlertMsgItem
                {
                    Html = html,
                    AlertType = string.IsNullOrWhiteSpace(alertType) ? "danger" : alertType
                }
            };
        }

        public static IEnumerable<AlertMsgItem> Concat(params IEnumerable<AlertMsgItem>[] sequences)
        {
            if (sequences == null || sequences.Length == 0)
                return Enumerable.Empty<AlertMsgItem>();
            return sequences.Where(s => s != null).SelectMany(s => s);
        }
    }
}
