using System.Web.Mvc;
using System.Web.Routing;

namespace WmsSystem
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            
            // API Routes
            routes.MapRoute(
                name: "Api",
                url: "api/{controller}/{action}/{id}",
                defaults: new { id = UrlParameter.Optional },
                namespaces: new[] { "WmsSystem.Controllers.Api" }
            );
            
            // Default route
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "WmsSystem.Controllers" }
            );
        }
    }
}
