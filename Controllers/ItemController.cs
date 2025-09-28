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
    public class ItemController : BaseController
    {
        private readonly WmsDbContext _context;
        private readonly ItemService _itemService;

        public ItemController()
        {
            _context = new WmsDbContext();
            _itemService = new ItemService(_context);
        }

        public async Task<ActionResult> Index(string category, bool? isConsumable)
        {
            var viewModel = new ItemIndexViewModel();
            
            // Get categories for filter
            viewModel.Categories = await _context.Items
                .Where(i => i.Active && !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Get items
            var query = _context.Items.Where(i => i.Active).AsQueryable();
            
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(i => i.Category == category);
                viewModel.SelectedCategory = category;
            }
            
            if (isConsumable.HasValue)
            {
                query = query.Where(i => i.IsConsumable == isConsumable.Value);
                viewModel.SelectedIsConsumable = isConsumable.Value;
            }

            viewModel.Items = await query
                .OrderBy(i => i.Code)
                .ToListAsync();

            return View(viewModel);
        }

        public ActionResult Create()
        {
            return View(new Item());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Item item)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _itemService.CreateItemAsync(item);
                    TempData["Success"] = "Item created successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            
            return View(item);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return HttpNotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Item item)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _itemService.UpdateItemAsync(item);
                    TempData["Success"] = "Item updated successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            
            return View(item);
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                await _itemService.DeleteItemAsync(id);
                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> Search(string term)
        {
            var items = await _context.Items
                .Where(i => i.Active && (i.Code.Contains(term) || i.Name.Contains(term)))
                .Take(10)
                .Select(i => new { 
                    id = i.Id, 
                    text = $"{i.Code} - {i.Name}",
                    code = i.Code,
                    name = i.Name,
                    uom = i.UoM
                })
                .ToListAsync();
                
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}
