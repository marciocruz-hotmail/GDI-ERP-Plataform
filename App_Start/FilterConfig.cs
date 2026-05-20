using System.Web;
using System.Web.Mvc;

namespace GDI_ERP_Plataform
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
