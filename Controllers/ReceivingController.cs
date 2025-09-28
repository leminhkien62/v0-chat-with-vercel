using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WmsSystem.Data;
using WmsSystem.Extensions;
using WmsSystem.Filters;
using WmsSystem.Services;
using WmsSystem.ViewModels;

namespace WmsSystem.Controllers
{
    [Authorize(Roles = "Admin,Store")]
    [WarehouseAccess]
    public class ReceivingController : BaseController
    {
        private readonly WmsDbContext _context;
        private readonly ReceivingService _receivingService;
        private readonly ErpIntegrationService _erpService;

        public ReceivingController()
        {
            _context = new WmsDbContext();
            _receivingService = new ReceivingService(_context);
            _erpService = new ErpIntegrationService();
        }

        public async Task<ActionResult> Index()
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            
            var viewModel = new ReceivingIndexViewModel();
            
            // Get pending POs
            viewModel.PendingPos = await _context.PoHeaders
                .Include(p => p.PoLines.Select(pl => pl.Item))
                .Where(p => p.PoLines.Any(pl => pl.QtyReceived < pl.QtyOrdered))
                .OrderByDescending(p => p.PoDate)
                .Take(20)
                .ToListAsync();

            // Get recent receipts
            viewModel.RecentReceipts = await _context.Transactions
                .Include(t => t.Item)
                .Include(t => t.ToLocation.Warehouse)
                .Where(t => t.Type == Models.TransactionType.Receive)
                .Where(t => allowedWarehouseIds.Count == 0 || allowedWarehouseIds.Contains(t.ToLocation.WarehouseId))
                .OrderByDescending(t => t.Ts)
                .Take(10)
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<ActionResult> SyncPurchaseOrders()
        {
            try
            {
                var syncedCount = await _erpService.SyncPurchaseOrdersAsync();
                TempData["Success"] = $"Successfully synced {syncedCount} purchase orders from ERP.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error syncing purchase orders: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> ReceivePo(int poId)
        {
            var po = await _context.PoHeaders
                .Include(p => p.PoLines.Select(pl => pl.Item))
                .FirstOrDefaultAsync(p => p.Id == poId);

            if (po == null)
                return HttpNotFound();

            var viewModel = new ReceivePoViewModel
            {
                Po = po,
                ReceiveLines = po.PoLines.Where(pl => pl.QtyReceived < pl.QtyOrdered)
                    .Select(pl => new ReceiveLineViewModel
                    {
                        PoLineId = pl.Id,
                        Item = pl.Item,
                        QtyOrdered = pl.QtyOrdered,
                        QtyReceived = pl.QtyReceived,
                        QtyToReceive = pl.QtyOrdered - pl.QtyReceived
                    }).ToList()
            };

            await PopulateLocationDropdown(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReceivePo(ReceivePoViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _receivingService.ReceivePurchaseOrderAsync(viewModel);
                    TempData["Success"] = $"Successfully received items. LPN: {result.LpnCode}";
                    return RedirectToAction("PrintLpn", new { lpnId = result.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // Reload data on error
            var po = await _context.PoHeaders
                .Include(p => p.PoLines.Select(pl => pl.Item))
                .FirstOrDefaultAsync(p => p.Id == viewModel.Po.Id);
            viewModel.Po = po;

            await PopulateLocationDropdown(viewModel);
            return View(viewModel);
        }

        public async Task<ActionResult> PrintLpn(int lpnId)
        {
            var lpn = await _context.Lpns
                .Include(l => l.Location.Warehouse)
                .Include(l => l.LpnItems.Select(li => li.Item))
                .FirstOrDefaultAsync(l => l.Id == lpnId);

            if (lpn == null)
                return HttpNotFound();

            var viewModel = new PrintLpnViewModel
            {
                Lpn = lpn,
                QrCodeData = _receivingService.GenerateQrCodeData(lpn)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<ActionResult> GenerateLpnPdf(int lpnId, string paperSize = "A6")
        {
            try
            {
                var pdfBytes = await _receivingService.GenerateLpnPdfAsync(lpnId, paperSize);
                var fileName = $"LPN_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating PDF: {ex.Message}";
                return RedirectToAction("PrintLpn", new { lpnId });
            }
        }

        public async Task<ActionResult> Putaway()
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            
            var openLpns = await _context.Lpns
                .Include(l => l.Location.Warehouse)
                .Include(l => l.LpnItems.Select(li => li.Item))
                .Where(l => !l.Closed)
                .Where(l => allowedWarehouseIds.Count == 0 || allowedWarehouseIds.Contains(l.Location.WarehouseId))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(openLpns);
        }

        public async Task<ActionResult> PutawayLpn(int lpnId)
        {
            var lpn = await _context.Lpns
                .Include(l => l.Location.Warehouse)
                .Include(l => l.LpnItems.Select(li => li.Item))
                .FirstOrDefaultAsync(l => l.Id == lpnId);

            if (lpn == null)
                return HttpNotFound();

            var viewModel = new PutawayLpnViewModel
            {
                Lpn = lpn,
                CurrentLocationId = lpn.LocationId
            };

            await PopulateLocationDropdown(viewModel, lpn.Location.WarehouseId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PutawayLpn(PutawayLpnViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _receivingService.PutawayLpnAsync(viewModel.Lpn.Id, viewModel.TargetLocationId, User.Identity.Name);
                    TempData["Success"] = "LPN putaway completed successfully.";
                    return RedirectToAction("Putaway");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // Reload data on error
            var lpn = await _context.Lpns
                .Include(l => l.Location.Warehouse)
                .Include(l => l.LpnItems.Select(li => li.Item))
                .FirstOrDefaultAsync(l => l.Id == viewModel.Lpn.Id);
            viewModel.Lpn = lpn;

            await PopulateLocationDropdown(viewModel, lpn.Location.WarehouseId);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> SearchPo(string term)
        {
            var pos = await _context.PoHeaders
                .Where(p => p.PoNo.Contains(term) || p.Supplier.Contains(term))
                .Where(p => p.PoLines.Any(pl => pl.QtyReceived < pl.QtyOrdered))
                .Take(10)
                .Select(p => new {
                    id = p.Id,
                    text = $"{p.PoNo} - {p.Supplier}",
                    poNo = p.PoNo,
                    supplier = p.Supplier,
                    poDate = p.PoDate
                })
                .ToListAsync();

            return Json(pos, JsonRequestBehavior.AllowGet);
        }

        private async Task PopulateLocationDropdown(dynamic viewModel, int? warehouseId = null)
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            var query = _context.Locations.Include(l => l.Warehouse).Where(l => !l.Locked);

            if (allowedWarehouseIds.Any())
                query = query.Where(l => allowedWarehouseIds.Contains(l.WarehouseId));

            if (warehouseId.HasValue)
                query = query.Where(l => l.WarehouseId == warehouseId.Value);

            var locations = await query
                .OrderBy(l => l.Warehouse.Code)
                .ThenBy(l => l.Code)
                .ToListAsync();

            viewModel.Locations = new SelectList(locations, "Id", "Code");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}
