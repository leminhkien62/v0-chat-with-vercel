using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WmsSystem.Data;
using WmsSystem.ViewModels;

namespace WmsSystem.Services
{
    public class PickingService
    {
        private readonly WmsDbContext _context;

        public PickingService(WmsDbContext context)
        {
            _context = context;
        }

        public async Task<List<PickListItemViewModel>> GeneratePickListAsync(int itemId, decimal requestedQty, List<int> allowedWarehouseIds, bool useFifo = true)
        {
            var pickList = new List<PickListItemViewModel>();
            
            // Get available stock with FIFO/FEFO ordering
            var stockQuery = _context.Stocks
                .Include(s => s.Item)
                .Include(s => s.Location.Warehouse)
                .Where(s => s.ItemId == itemId && s.QtyOnHand > s.QtyAllocated);

            // Apply warehouse filter if specified
            if (allowedWarehouseIds.Any())
                stockQuery = stockQuery.Where(s => allowedWarehouseIds.Contains(s.Location.WarehouseId));

            // Order by FEFO (First-Expired-First-Out) then FIFO (First-In-First-Out)
            var availableStock = await stockQuery
                .OrderBy(s => s.ExpiryDate ?? DateTime.MaxValue)  // FEFO: Expiry date first
                .ThenBy(s => s.ReceivedDate)                      // FIFO: Received date second
                .ThenBy(s => s.Location.Code)                     // Consistent ordering
                .ToListAsync();

            decimal remainingQty = requestedQty;

            foreach (var stock in availableStock)
            {
                if (remainingQty <= 0) break;

                var availableQty = stock.QtyOnHand - stock.QtyAllocated;
                if (availableQty <= 0) continue;

                var qtyToPick = Math.Min(remainingQty, availableQty);

                var pickItem = new PickListItemViewModel
                {
                    StockId = stock.Id,
                    ItemCode = stock.Item.Code,
                    ItemName = stock.Item.Name,
                    LocationCode = stock.Location.Code,
                    WarehouseCode = stock.Location.Warehouse.Code,
                    Lot = stock.Lot,
                    Serial = stock.Serial,
                    ExpiryDate = stock.ExpiryDate,
                    ReceivedDate = stock.ReceivedDate,
                    AvailableQty = availableQty,
                    SuggestedQty = qtyToPick,
                    QtyToPick = qtyToPick,
                    Priority = GetPickPriority(stock),
                    IsExpiringSoon = IsExpiringSoon(stock.ExpiryDate)
                };

                pickList.Add(pickItem);
                remainingQty -= qtyToPick;
            }

            return pickList;
        }

        public async Task<List<PickListItemViewModel>> GeneratePickListForMultipleItemsAsync(List<RequestLineViewModel> requestLines, List<int> allowedWarehouseIds)
        {
            var allPickItems = new List<PickListItemViewModel>();

            foreach (var requestLine in requestLines)
            {
                var pickItems = await GeneratePickListAsync(requestLine.ItemId, requestLine.Qty, allowedWarehouseIds);
                allPickItems.AddRange(pickItems);
            }

            // Group by location for efficient picking
            return allPickItems
                .OrderBy(p => p.WarehouseCode)
                .ThenBy(p => p.LocationCode)
                .ThenBy(p => p.Priority)
                .ToList();
        }

        public async Task<PickingAnalysisViewModel> AnalyzePickingEfficiencyAsync(int itemId, decimal requestedQty, List<int> allowedWarehouseIds)
        {
            var pickList = await GeneratePickListAsync(itemId, requestedQty, allowedWarehouseIds);
            
            var analysis = new PickingAnalysisViewModel
            {
                ItemId = itemId,
                RequestedQty = requestedQty,
                TotalAvailable = pickList.Sum(p => p.AvailableQty),
                LocationsRequired = pickList.Count,
                WarehousesInvolved = pickList.Select(p => p.WarehouseCode).Distinct().Count(),
                ExpiringSoonCount = pickList.Count(p => p.IsExpiringSoon),
                CanFulfillCompletely = pickList.Sum(p => p.AvailableQty) >= requestedQty,
                PickingComplexity = CalculatePickingComplexity(pickList)
            };

            return analysis;
        }

        private int GetPickPriority(Models.Stock stock)
        {
            int priority = 0;

            // Higher priority for items expiring soon
            if (IsExpiringSoon(stock.ExpiryDate))
                priority += 100;

            // Higher priority for older stock (FIFO)
            var daysSinceReceived = (DateTime.Now - stock.ReceivedDate).Days;
            priority += Math.Min(daysSinceReceived, 50);

            return priority;
        }

        private bool IsExpiringSoon(DateTime? expiryDate, int warningDays = 30)
        {
            if (!expiryDate.HasValue) return false;
            return expiryDate.Value <= DateTime.Now.AddDays(warningDays);
        }

        private string CalculatePickingComplexity(List<PickListItemViewModel> pickList)
        {
            if (pickList.Count <= 1) return "Simple";
            if (pickList.Count <= 3) return "Moderate";
            if (pickList.Select(p => p.WarehouseCode).Distinct().Count() > 1) return "Complex";
            return "Moderate";
        }
    }
}
