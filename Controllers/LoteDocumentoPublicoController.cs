using GdiPlataform.Services;
using System;
using System.Linq;
using System.Web.Http;

namespace GdiPlataform.Controllers
{
    /// <summary>
    /// API pública (sem autenticação) para redirecionar o cliente ao PDF do lote (QR / link em etiqueta).
    /// Legado: GDI-PortalCliente-Plataform — migrado para este monólito (host portalflightx.com).
    /// </summary>
    [AllowAnonymous]
    [RoutePrefix("api/public/lote-documento")]
    public class LoteDocumentoPublicoController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get([FromUri] string idLote, [FromUri] string format = null)
        {
            var urlFallback = LotePublicDocumentUrlService.ObterUrlFallbackConfigurada();

            if (string.IsNullOrWhiteSpace(idLote))
            {
                return Redirect(new Uri(urlFallback));
            }

            if (!int.TryParse(idLote.Trim(), out var idLoteInt))
            {
                return Redirect(new Uri(urlFallback));
            }

            try
            {
                var entityConnectionName = ResolverConnectionNamePorHost();
                using (var svc = new LotePublicDocumentUrlService(entityConnectionName))
                {
                    var resultado = svc.Resolver(idLoteInt);

                    switch (resultado.Status)
                    {
                        case LotePublicDocumentUrlStatus.ResolvidoBanco:
                            return Redirect(new Uri(resultado.Url));

                        default:
                            return Redirect(new Uri(urlFallback));
                    }
                }
            }
            catch (Exception)
            {
                return Redirect(new Uri(urlFallback));
            }
        }

        /// <summary>
        /// Mesma regra de tenant que <see cref="JobServerController"/> / portal (subdomínio → connection EF).
        /// </summary>
        private string ResolverConnectionNamePorHost()
        {
            var hostHeader = Request?.Headers?.Host ?? string.Empty;
            var dominio = hostHeader.ToLower().Trim();
            var host = dominio.Replace("http://", "").Replace("https://", "").Replace("www.", "").Trim();
            if (host.IndexOf(":", StringComparison.Ordinal) >= 0)
            {
                host = host.Substring(0, host.IndexOf(":", StringComparison.Ordinal));
            }

            var index = host.IndexOf(".", StringComparison.Ordinal);
            if (index < 0)
            {
                host = host.Trim();
            }
            else
            {
                host = host.Substring(0, index);
            }

            var tenant = UserIdentityController.SetTenants()
                .FirstOrDefault(t => t.subDominio.Equals(host, StringComparison.OrdinalIgnoreCase));

            return tenant?.database;
        }
    }
}
