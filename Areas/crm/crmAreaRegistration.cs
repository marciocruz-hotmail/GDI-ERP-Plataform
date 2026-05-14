using System.Web.Mvc;

namespace GdiPlataform.Areas.crm
{
    public class crmAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "crm"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "crm_default",
                "crm/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
