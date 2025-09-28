using System.Web.Mvc;
using WmsSystem.Filters;

namespace WmsSystem.Controllers
{
    [AuditLog]
    public abstract class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Add common logic here if needed
            base.OnActionExecuting(filterContext);
        }
    }
}
