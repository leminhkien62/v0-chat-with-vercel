using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace WmsSystem.Extensions
{
    public static class ControllerExtensions
    {
        public static List<int> GetAllowedWarehouseIds(this Controller controller)
        {
            if (controller.User.IsInRole("Admin"))
                return new List<int>(); // Admin can access all warehouses

            var allowedIds = HttpContext.Current.Items["AllowedWarehouseIds"] as List<int>;
            return allowedIds ?? new List<int>();
        }

        public static bool HasWarehouseAccess(this Controller controller, int warehouseId)
        {
            if (controller.User.IsInRole("Admin"))
                return true;

            var allowedIds = controller.GetAllowedWarehouseIds();
            return allowedIds.Contains(warehouseId);
        }
    }
}
