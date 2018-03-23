using System.Web.Mvc;

namespace CapstoneProject.Areas.MarkComp
{
    public class MarkCompAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "MarkComp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "MarkComp_default",
                "MarkComp/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}