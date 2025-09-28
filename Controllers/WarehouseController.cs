using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using WmsSystem.Data;
using WmsSystem.Models;
using WmsSystem.Services;

namespace WmsSystem.Controllers
{
    [Authorize]
    public class WarehouseController : BaseController
    {
        private readonly WmsDbContext _context;
        private readonly WarehouseService _warehouseService;

        public WarehouseController()
        {
            _context = new WmsDbContext();
            _warehouseService = new WarehouseService(_context);
        }

        public async Task<ActionResult> Index()
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.Active)
                .OrderBy(w => w.Code)
                .ToListAsync();
            
            return View(warehouses);
        }

        public ActionResult Create()
        {
            return View(new Warehouse());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _warehouseService.CreateWarehouseAsync(warehouse);
                    TempData["Success"] = "Warehouse created successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            
            return View(warehouse);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
                return HttpNotFound();

            return View(warehouse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _warehouseService.UpdateWarehouseAsync(warehouse);
                    TempData["Success"] = "Warehouse updated successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            
            return View(warehouse);
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                await _warehouseService.DeleteWarehouseAsync(id);
                return Json(new { success = true, message = "Warehouse deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}
