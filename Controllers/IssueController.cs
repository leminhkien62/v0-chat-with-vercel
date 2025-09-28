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
    public class IssueController : BaseController
    {
        private readonly WmsDbContext _context;
        private readonly IssueService _issueService;
        private readonly PickingService _pickingService;

        public IssueController()
        {
            _context = new WmsDbContext();
            _issueService = new IssueService(_context);
            _pickingService = new PickingService(_context);
        }

        public async Task<ActionResult> Index()
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            
            var viewModel = new IssueIndexViewModel();
            
            // Get pending requests
            viewModel.PendingRequests = await _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Department)
                .Where(r => r.Status == Models.RequestStatus.New || r.Status == Models.RequestStatus.Processing)
                .OrderBy(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Get recent issues
            viewModel.RecentIssues = await _context.Transactions
                .Include(t => t.Item)
                .Include(t => t.FromLocation.Warehouse)
                .Where(t => t.Type == Models.TransactionType.Issue)
                .Where(t => allowedWarehouseIds.Count == 0 || allowedWarehouseIds.Contains(t.FromLocation.WarehouseId))
                .OrderByDescending(t => t.Ts)
                .Take(10)
                .ToListAsync();

            return View(viewModel);
        }

        public ActionResult CreateIssue()
        {
            var viewModel = new CreateIssueViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateIssue(CreateIssueViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var issueId = await _issueService.CreateManualIssueAsync(viewModel, User.Identity.Name);
                    TempData["Success"] = "Issue created successfully.";
                    return RedirectToAction("ProcessIssue", new { id = issueId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(viewModel);
        }

        public async Task<ActionResult> ProcessRequest(int requestId)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Department)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return HttpNotFound();

            if (request.Status != Models.RequestStatus.New)
            {
                TempData["Error"] = "Request has already been processed.";
                return RedirectToAction("Index");
            }

            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            var pickList = await _pickingService.GeneratePickListAsync(request.ItemId, request.Qty, allowedWarehouseIds);

            var viewModel = new ProcessRequestViewModel
            {
                Request = request,
                PickList = pickList,
                TotalAvailable = pickList.Sum(p => p.AvailableQty)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessRequest(ProcessRequestViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _issueService.ProcessRequestAsync(viewModel, User.Identity.Name);
                    TempData["Success"] = "Request processed successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // Reload data on error
            var request = await _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Department)
                .FirstOrDefaultAsync(r => r.Id == viewModel.Request.Id);
            viewModel.Request = request;

            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            viewModel.PickList = await _pickingService.GeneratePickListAsync(request.ItemId, request.Qty, allowedWarehouseIds);

            return View(viewModel);
        }

        public async Task<ActionResult> Move()
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            
            var viewModel = new MoveIndexViewModel();
            
            // Get recent moves
            viewModel.RecentMoves = await _context.Transactions
                .Include(t => t.Item)
                .Include(t => t.FromLocation.Warehouse)
                .Include(t => t.ToLocation.Warehouse)
                .Where(t => t.Type == Models.TransactionType.Move)
                .Where(t => allowedWarehouseIds.Count == 0 || 
                           allowedWarehouseIds.Contains(t.FromLocation.WarehouseId) ||
                           allowedWarehouseIds.Contains(t.ToLocation.WarehouseId))
                .OrderByDescending(t => t.Ts)
                .Take(20)
                .ToListAsync();

            return View(viewModel);
        }

        public ActionResult CreateMove()
        {
            var viewModel = new CreateMoveViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateMove(CreateMoveViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _issueService.CreateMoveAsync(viewModel, User.Identity.Name);
                    TempData["Success"] = "Move operation completed successfully.";
                    return RedirectToAction("Move");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(viewModel);
        }

        public async Task<ActionResult> PickList(int itemId, decimal qty)
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            var pickList = await _pickingService.GeneratePickListAsync(itemId, qty, allowedWarehouseIds);
            
            var item = await _context.Items.FindAsync(itemId);
            
            var viewModel = new PickListViewModel
            {
                Item = item,
                RequestedQty = qty,
                PickList = pickList,
                TotalAvailable = pickList.Sum(p => p.AvailableQty)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetItemStock(int itemId)
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            
            var stock = await _context.Stocks
                .Include(s => s.Location.Warehouse)
                .Where(s => s.ItemId == itemId && s.QtyOnHand > s.QtyAllocated)
                .Where(s => allowedWarehouseIds.Count == 0 || allowedWarehouseIds.Contains(s.Location.WarehouseId))
                .Select(s => new {
                    locationId = s.LocationId,
                    locationCode = s.Location.Code,
                    warehouseCode = s.Location.Warehouse.Code,
                    availableQty = s.QtyOnHand - s.QtyAllocated,
                    lot = s.Lot,
                    serial = s.Serial,
                    expiryDate = s.ExpiryDate,
                    receivedDate = s.ReceivedDate
                })
                .OrderBy(s => s.expiryDate ?? DateTime.MaxValue)
                .ThenBy(s => s.receivedDate)
                .ToListAsync();

            return Json(stock, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetLocationStock(int locationId)
        {
            var allowedWarehouseIds = this.GetAllowedWarehouseIds();
            
            var stock = await _context.Stocks
                .Include(s => s.Item)
                .Include(s => s.Location.Warehouse)
                .Where(s => s.LocationId == locationId && s.QtyOnHand > 0)
                .Where(s => allowedWarehouseIds.Count == 0 || allowedWarehouseIds.Contains(s.Location.WarehouseId))
                .Select(s => new {
                    itemId = s.ItemId,
                    itemCode = s.Item.Code,
                    itemName = s.Item.Name,
                    availableQty = s.QtyOnHand - s.QtyAllocated,
                    lot = s.Lot,
                    serial = s.Serial,
                    expiryDate = s.ExpiryDate
                })
                .ToListAsync();

            return Json(stock, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}
