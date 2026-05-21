using GdiPlataform.Lib;
using System;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Security
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                || filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
            {
                return;
            }

            if (CachePersister.userIdentity == null)
            {
                // Se o usuário não estava conectado
                if (HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Equals(String.Empty))
                {
                    if (IsAjaxRequest(filterContext))
                    {
                        SetJsonUnauthorized(filterContext, mensagem: "Sessão expirada. Efetue nova conexão.");
                    }
                    else
                    {
                        filterContext.Result = new RedirectToRouteResult
                            (new System.Web.Routing.RouteValueDictionary(new { controller = "UserIdentity", action = "Index", area = String.Empty }));
                    }
                }
                else // Se o usuário foi desconectado
                {
                    if (IsAjaxRequest(filterContext))
                    {
                        SetJsonUnauthorized(filterContext, mensagem: "Sessão expirada. Efetue nova conexão.");
                    }
                    else
                    {
                        GdiPlataform.Lib.LibFlashMessage.SetInfo(
                            filterContext.Controller as Controller,
                            "Tempo de conexão expirado, Efetue nova conexão");
                        filterContext.Result = new RedirectToRouteResult
                            (new System.Web.Routing.RouteValueDictionary(new { controller = "UserIdentity", action = "Index", area = String.Empty }));
                    }
                }
            }
            else
            {
                CustomPrincipal customPrincipal = new CustomPrincipal(CachePersister.userIdentity);
                if (!customPrincipal.IsInRole(Roles))
                {
                    string url = filterContext?.HttpContext?.Request?.Url?.AbsolutePath ?? "?";
                    string usuario = CachePersister.userIdentity?.Username ?? "?";
                    LibLogger.Warn($"[Auth] Acesso negado | usuario={usuario} | role={Roles} | url={url}");

                    if (IsAjaxRequest(filterContext))
                    {
                        SetJsonForbidden(filterContext, mensagem: "Acesso negado.");
                    }
                    else
                    {
                        filterContext.Result = new RedirectToRouteResult
                            (new System.Web.Routing.RouteValueDictionary(new { controller = "AcessoNegado", action = "Index", area = String.Empty }));
                    }
                }
            }
        }

        private static bool IsAjaxRequest(AuthorizationContext filterContext)
        {
            return filterContext?.HttpContext?.Request != null
                && filterContext.HttpContext.Request.IsAjaxRequest();
        }

        private static void SetJsonUnauthorized(AuthorizationContext filterContext, string mensagem)
        {
            var response = filterContext.HttpContext.Response;
            response.StatusCode = 401;
            response.ContentType = "application/json";
            response.TrySkipIisCustomErrors = true;
            response.SuppressFormsAuthenticationRedirect = true;

            filterContext.Result = new JsonResult
            {
                Data = new
                {
                    sucesso = false,
                    sessaoExpirada = true,
                    mensagem = mensagem ?? "Sessão expirada. Efetue nova conexão."
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        private static void SetJsonForbidden(AuthorizationContext filterContext, string mensagem)
        {
            var response = filterContext.HttpContext.Response;
            response.StatusCode = 403;
            response.ContentType = "application/json";
            response.TrySkipIisCustomErrors = true;

            filterContext.Result = new JsonResult
            {
                Data = new
                {
                    sucesso = false,
                    acessoNegado = true,
                    mensagem = mensagem ?? "Acesso negado."
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
    }
}
