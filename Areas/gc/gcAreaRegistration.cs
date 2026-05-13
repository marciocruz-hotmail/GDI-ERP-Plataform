using System.Web.Mvc;

namespace GdiPlataform.Areas.gc
{
    public class gcAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "gc";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "gc_default",
                "gc/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );

        }
    }
}