using System.Web;
using System.Web.Mvc;
using GdiPlataform.Lib;

namespace GDI_ERP_Plataform
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new GdiPageScriptsActionFilter());
        }
    }
}
