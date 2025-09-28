using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WmsSystem.Models;
using WmsSystem.ViewModels;

namespace WmsSystem.Services
{
    public class RequestService : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public RequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<SelectListItem> GetDepartments()
        {
            return _context.Departments
                .Where(d => d.Active)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToList();
        }

        public List<SelectListItem> GetAvailableItems()
        {
            return _context.Items
                .Where(i => i.Active)
                .OrderBy(i => i.Code)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Code + " - " + i.Name
                })
                .ToList();
        }

        public List<RequestListViewModel> GetPendingRequests(List<int> allowedWarehouses)
        {
            // Get requests for items that exist in allowed warehouses
            var itemsInAllowedWarehouses = _context.Stock
                .Include(s => s.Location)
                .Where(s => allowedWarehouses.Contains(s.Location.WarehouseId))
                .Select(s => s.ItemId)
                .Distinct()
                .ToList();

            return _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Department)
                .Where(r => r.Status != RequestStatus.Completed && 
                           r.Status != RequestStatus.Rejected &&
                           itemsInAllowedWarehouses.Contains(r.ItemId))
                .OrderBy(r => r.CreatedAt)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    Requester = r.Requester,
                    DepartmentName = r.Department.Name,
                    ItemCode = r.Item.Code,
                    ItemName = r.Item.Name,
                    Qty = r.Qty,
                    UoM = r.Item.UoM,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    Note = r.Note
                })
                .ToList();
        }

        public bool CanProcessRequest(int requestId, List<int> allowedWarehouses)
        {
            var request = _context.Requests.Find(requestId);
            if (request == null) return false;

            // Check if item exists in any allowed warehouse
            return _context.Stock
                .Include(s => s.Location)
                .Any(s => s.ItemId == request.ItemId && 
                         allowedWarehouses.Contains(s.Location.WarehouseId));
        }

        public ProcessRequestViewModel GetProcessRequestViewModel(int requestId)
        {
            var request = _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Department)
                .FirstOrDefault(r => r.Id == requestId);

            if (request == null) return null;

            // Get available stock for this item
            var stockLocations = _context.Stock
                .Include(s => s.Location)
                .Include(s => s.Location.Warehouse)
                .Where(s => s.ItemId == request.ItemId && s.QtyOnHand > 0)
                .OrderBy(s => s.ReceivedDate) // FIFO
                .ThenBy(s => s.ExpiryDate) // FEFO
                .ToList();

            var pickList = new List<PickListItemViewModel>();
            var remainingQty = request.Qty;
            var priority = 1;

            foreach (var stock in stockLocations)
            {
                if (remainingQty <= 0) break;

                var suggestedQty = Math.Min(remainingQty, stock.QtyOnHand);
                var isExpiringSoon = stock.ExpiryDate.HasValue && 
                                   stock.ExpiryDate.Value <= DateTime.Today.AddDays(30);

                pickList.Add(new PickListItemViewModel
                {
                    StockId = stock.Id,
                    LocationCode = stock.Location.Code,
                    WarehouseCode = stock.Location.Warehouse.Code,
                    Lot = stock.Lot,
                    Serial = stock.Serial,
                    ExpiryDate = stock.ExpiryDate,
                    ReceivedDate = stock.ReceivedDate,
                    AvailableQty = stock.QtyOnHand,
                    SuggestedQty = suggestedQty,
                    PickQty = suggestedQty,
                    Priority = priority++,
                    IsExpiringSoon = isExpiringSoon
                });

                remainingQty -= suggestedQty;
            }

            return new ProcessRequestViewModel
            {
                Request = request,
                PickList = pickList,
                TotalAvailable = stockLocations.Sum(s => s.QtyOnHand)
            };
        }

        public ServiceResult ProcessRequest(int requestId, string action, string notes, string userName, List<int> allowedWarehouses)
        {
            var request = _context.Requests.Find(requestId);
            if (request == null)
                return new ServiceResult { Success = false, Message = "Request not found" };

            if (!CanProcessRequest(requestId, allowedWarehouses))
                return new ServiceResult { Success = false, Message = "Access denied" };

            if (action == "reject")
            {
                request.Status = RequestStatus.Rejected;
                request.ProcessedAt = DateTime.Now;
                request.ProcessedBy = userName;
                request.ProcessedNote = notes;
                
                _context.SaveChanges();
                return new ServiceResult { Success = true, Message = "Request rejected successfully" };
            }
            else if (action == "process")
            {
                // This would integrate with the Issue system
                // For now, just mark as completed
                request.Status = RequestStatus.Completed;
                request.ProcessedAt = DateTime.Now;
                request.ProcessedBy = userName;
                request.ProcessedNote = notes;
                
                _context.SaveChanges();
                return new ServiceResult { Success = true, Message = "Request processed successfully" };
            }

            return new ServiceResult { Success = false, Message = "Invalid action" };
        }

        public ItemAvailabilityViewModel GetItemAvailability(int itemId, List<int> allowedWarehouses)
        {
            var item = _context.Items.Find(itemId);
            if (item == null) return null;

            var totalStock = _context.Stock
                .Include(s => s.Location)
                .Where(s => s.ItemId == itemId && 
                           allowedWarehouses.Contains(s.Location.WarehouseId))
                .Sum(s => (decimal?)s.QtyOnHand) ?? 0;

            var locations = _context.Stock
                .Include(s => s.Location)
                .Include(s => s.Location.Warehouse)
                .Where(s => s.ItemId == itemId && 
                           s.QtyOnHand > 0 &&
                           allowedWarehouses.Contains(s.Location.WarehouseId))
                .Select(s => new StockLocationInfo
                {
                    WarehouseName = s.Location.Warehouse.Name,
                    LocationCode = s.Location.Code,
                    Qty = s.QtyOnHand,
                    Lot = s.Lot,
                    ExpiryDate = s.ExpiryDate
                })
                .ToList();

            return new ItemAvailabilityViewModel
            {
                ItemCode = item.Code,
                ItemName = item.Name,
                UoM = item.UoM,
                TotalAvailable = totalStock,
                Locations = locations
            };
        }

        public void Dispose()
        {
            // Context is managed by controller
        }
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class StockLocationInfo
    {
        public string WarehouseName { get; set; }
        public string LocationCode { get; set; }
        public decimal Qty { get; set; }
        public string Lot { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
