using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using WmsSystem.Filters;

namespace WmsSystem
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            
            // Register global filters
            GlobalFilters.Filters.Add(new AuditLogAttribute());
            GlobalFilters.Filters.Add(new HandleErrorAttribute());
        }
    }
}
