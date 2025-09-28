using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using WmsSystem.Data;
using WmsSystem.Models;
using WmsSystem.Services;
using WmsSystem.ViewModels;

namespace WmsSystem.Controllers
{
    [Authorize]
    public class LocationController : BaseController
    {
        private readonly WmsDbContext _context;
        private readonly LocationService _locationService;

        public LocationController()
        {
            _context = new WmsDbContext();
            _locationService = new LocationService(_context);
        }

        public async Task<ActionResult> Index(int? warehouseId)
        {
            var viewModel = new LocationIndexViewModel();
            
            // Get warehouses for filter
            viewModel.Warehouses = await _context.Warehouses
                .Where(w => w.Active)
                .OrderBy(w => w.Code)
                .ToListAsync();

            // Get locations
            var query = _context.Locations.Include(l => l.Warehouse).AsQueryable();
            
            if (warehouseId.HasValue)
            {
                query = query.Where(l => l.WarehouseId == warehouseId.Value);
                viewModel.SelectedWarehouseId = warehouseId.Value;
            }

            viewModel.Locations = await query
                .OrderBy(l => l.Warehouse.Code)
                .ThenBy(l => l.Zone)
                .ThenBy(l => l.Aisle)
                .ThenBy(l => l.Rack)
                .ThenBy(l => l.Bin)
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<ActionResult> Create()
        {
            var viewModel = new LocationCreateViewModel();
            await PopulateWarehouseDropdown(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LocationCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _locationService.CreateLocationAsync(viewModel.Location);
                    TempData["Success"] = "Location created successfully.";
                    return RedirectToAction("Index", new { warehouseId = viewModel.Location.WarehouseId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            
            await PopulateWarehouseDropdown(viewModel);
            return View(viewModel);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var location = await _context.Locations
                .Include(l => l.Warehouse)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (location == null)
                return HttpNotFound();

            var viewModel = new LocationEditViewModel { Location = location };
            await PopulateWarehouseDropdown(viewModel);
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LocationEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _locationService.UpdateLocationAsync(viewModel.Location);
                    TempData["Success"] = "Location updated successfully.";
                    return RedirectToAction("Index", new { warehouseId = viewModel.Location.WarehouseId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            
            await PopulateWarehouseDropdown(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                await _locationService.DeleteLocationAsync(id);
                return Json(new { success = true, message = "Location deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ToggleLock(int id)
        {
            try
            {
                await _locationService.ToggleLockAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateWarehouseDropdown(dynamic viewModel)
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.Active)
                .OrderBy(w => w.Code)
                .ToListAsync();
                
            viewModel.Warehouses = new SelectList(warehouses, "Id", "Name");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}
