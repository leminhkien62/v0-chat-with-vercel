using System;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using WmsSystem.Hubs;
using WmsSystem.Models;
using WmsSystem.Services;
using WmsSystem.ViewModels;

namespace WmsSystem.Controllers
{
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private readonly RequestService _requestService;

        public RequestController()
        {
            _requestService = new RequestService(db);
        }

        // PWA Self-Service Request Form (No authentication required)
        [AllowAnonymous]
        public ActionResult Create()
        {
            var viewModel = new CreateRequestViewModel
            {
                Departments = _requestService.GetDepartments(),
                Items = _requestService.GetAvailableItems()
            };
            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var request = new Request
                    {
                        DeptId = model.DeptId,
                        Requester = model.Requester,
                        ItemId = model.ItemId,
                        Qty = model.Qty,
                        Note = model.Note,
                        Status = RequestStatus.New,
                        CreatedAt = DateTime.Now
                    };

                    db.Requests.Add(request);
                    db.SaveChanges();

                    // Send real-time notification to warehouse staff
                    var hubContext = GlobalHost.ConnectionManager.GetHubContext<WmsHub>();
                    hubContext.Clients.All.newRequest(new
                    {
                        Id = request.Id,
                        Requester = request.Requester,
                        ItemCode = db.Items.Find(request.ItemId)?.Code,
                        Qty = request.Qty,
                        Department = db.Departments.Find(request.DeptId)?.Name,
                        CreatedAt = request.CreatedAt
                    });

                    ViewBag.Success = true;
                    ViewBag.RequestId = request.Id;
                    
                    // Clear form for new request
                    ModelState.Clear();
                    model = new CreateRequestViewModel
                    {
                        Departments = _requestService.GetDepartments(),
                        Items = _requestService.GetAvailableItems()
                    };
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Failed to submit request. Please try again.";
                    // Log error in production
                }
            }

            if (model.Departments == null)
            {
                model.Departments = _requestService.GetDepartments();
                model.Items = _requestService.GetAvailableItems();
            }

            return View(model);
        }

        // Management interface (requires authentication)
        [Authorize(Roles = "Admin,Store,Manager")]
        public ActionResult Index()
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var requests = _requestService.GetPendingRequests(allowedWarehouses);
            return View(requests);
        }

        [Authorize(Roles = "Admin,Store")]
        public ActionResult Process(int id)
        {
            var request = db.Requests
                .Include("Item")
                .Include("Department")
                .FirstOrDefault(r => r.Id == id);

            if (request == null)
                return HttpNotFound();

            // Check warehouse access
            var allowedWarehouses = GetAllowedWarehouses();
            var hasAccess = _requestService.CanProcessRequest(request.Id, allowedWarehouses);
            
            if (!hasAccess)
                return new HttpStatusCodeResult(403, "Access denied");

            var viewModel = _requestService.GetProcessRequestViewModel(request.Id);
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Store")]
        [ValidateAntiForgeryToken]
        public ActionResult Process(int id, string action, string notes)
        {
            try
            {
                var allowedWarehouses = GetAllowedWarehouses();
                var result = _requestService.ProcessRequest(id, action, notes, User.Identity.Name, allowedWarehouses);
                
                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = result.Message;
                    return RedirectToAction("Process", new { id });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing the request.";
                return RedirectToAction("Process", new { id });
            }
        }

        [HttpGet]
        public JsonResult GetItemInfo(int itemId)
        {
            var allowedWarehouses = GetAllowedWarehouses();
            var itemInfo = _requestService.GetItemAvailability(itemId, allowedWarehouses);
            return Json(itemInfo, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [AllowAnonymous]
        public JsonResult CheckRequestStatus(int requestId)
        {
            var request = db.Requests.Find(requestId);
            if (request == null)
                return Json(new { found = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                found = true,
                status = request.Status.ToString(),
                processedAt = request.ProcessedAt?.ToString("yyyy-MM-dd HH:mm"),
                note = request.ProcessedNote
            }, JsonRequestBehavior.AllowGet);
        }

        private List<int> GetAllowedWarehouses()
        {
            if (User.IsInRole("Admin"))
            {
                return db.Warehouses.Where(w => w.Active).Select(w => w.Id).ToList();
            }

            var userId = User.Identity.GetUserId();
            return db.UserWarehouses
                .Where(uw => uw.UserId == userId)
                .Select(uw => uw.WarehouseId)
                .ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _requestService?.Dispose();
                db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
