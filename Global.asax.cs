using GDI_ERP_Plataform.App_Start;
using GdiPlataform.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace GDI_ERP_Plataform
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            LookupDependencyConfig.Register();
        }

        protected void Application_Error()
        {
            Exception ex = Server.GetLastError();
            if (ex != null)
            {
                string url = Request?.Url?.AbsolutePath ?? "?";
                LibLogger.Error($"[Application_Error] {url}", ex);
            }
        }
    }
}
