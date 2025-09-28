using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WmsSystem.Data;

namespace WmsSystem.Filters
{
    public class WarehouseAccessAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = filterContext.HttpContext.User;
            
            if (user.Identity.IsAuthenticated && !user.IsInRole("Admin"))
            {
                using (var context = new WmsDbContext())
                {
                    var userId = user.Identity.GetUserId();
                    var allowedWarehouseIds = context.UserWarehouses
                        .Where(uw => uw.UserId == userId)
                        .Select(uw => uw.WarehouseId)
                        .ToList();

                    // Store allowed warehouse IDs in HttpContext for use in controllers
                    HttpContext.Current.Items["AllowedWarehouseIds"] = allowedWarehouseIds;
                }
            }
            
            base.OnActionExecuting(filterContext);
        }
    }
}
