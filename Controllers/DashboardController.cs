using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WmsSystem.Models;
using WmsSystem.Services;
using WmsSystem.ViewModels;

namespace WmsSystem.Controllers
{
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly DashboardService _dashboardService;

        public DashboardController()
        {
            _dashboardService = new DashboardService(db);
        }

        public ActionResult Index()
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var viewModel = _dashboardService.GetDashboardData(allowedWarehouses);
            return View(viewModel);
        }

        [HttpGet]
        public JsonResult GetKpiData()
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var data = _dashboardService.GetKpiData(allowedWarehouses);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTransactionChart(int days = 30)
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var data = _dashboardService.GetTransactionChartData(allowedWarehouses, days);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTopItemsChart(int top = 10)
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var data = _dashboardService.GetTopItemsChartData(allowedWarehouses, top);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetHeatmapData(int? warehouseId = null, string zone = null, string aisle = null)
        {
            var allowedWarehouses = GetAllowedWarehouses();
            
            // Filter by allowed warehouses
            if (warehouseId.HasValue && !allowedWarehouses.Contains(warehouseId.Value))
            {
                return Json(new { error = "Access denied to warehouse" }, JsonRequestBehavior.AllowGet);
            }

            var data = _dashboardService.GetHeatmapData(allowedWarehouses, warehouseId, zone, aisle);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetHeatmapFilters()
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var filters = _dashboardService.GetHeatmapFilters(allowedWarehouses);
            return Json(filters, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetRecentAlerts()
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var alerts = _dashboardService.GetRecentAlerts(allowedWarehouses);
            return Json(alerts, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dashboardService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
