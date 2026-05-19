using System;
using System.Web.Helpers;
using System.Web.Mvc;

namespace GdiPlataform.Security
{
    /// <summary>
    /// Valida token antiforgery em POST: campo de formulário (form.serialize / FormData)
    /// ou cabeçalho Ajax (GdiAjaxAntiForgeryHeaders em start.js).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class GdiValidateAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException(nameof(filterContext));
            }

            var request = filterContext.HttpContext.Request;
            var method = request.HttpMethod;
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                || string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase)
                || string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string formToken = request.Form["__RequestVerificationToken"];
            if (string.IsNullOrEmpty(formToken))
            {
                formToken = request.Headers["__RequestVerificationToken"];
            }
            if (string.IsNullOrEmpty(formToken))
            {
                formToken = request.Headers["RequestVerificationToken"];
            }
            if (string.IsNullOrEmpty(formToken))
            {
                formToken = request.Headers["X-RequestVerificationToken"];
            }

            if (string.IsNullOrEmpty(formToken))
            {
                throw new HttpAntiForgeryException("Token antiforgery obrigatório não foi enviado.");
            }

            string cookieName = AntiForgeryConfig.CookieName;
            string cookieToken = null;
            if (!string.IsNullOrEmpty(cookieName))
            {
                var cookie = request.Cookies[cookieName];
                if (cookie != null)
                {
                    cookieToken = cookie.Value;
                }
            }

            if (string.IsNullOrEmpty(cookieToken))
            {
                var fallbackCookie = request.Cookies["__RequestVerificationToken"];
                if (fallbackCookie != null)
                {
                    cookieToken = fallbackCookie.Value;
                }
            }

            AntiForgery.Validate(cookieToken, formToken);
        }
    }
}
