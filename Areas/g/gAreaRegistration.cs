using System.Web.Mvc;

namespace GdiPlataform.Areas.g
{
    public class gAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "g";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "g_default",
                "g/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}