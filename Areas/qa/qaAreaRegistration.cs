using System.Web.Mvc;

namespace GdiPlataform.Areas.qa
{
    public class qaAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "qa";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "qa_default",
                "qa/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}