using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WmsSystem.Models;
using WmsSystem.ViewModels;

namespace WmsSystem.Services
{
    public class DashboardService : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public DashboardViewModel GetDashboardData(List<int> allowedWarehouses)
        {
            var viewModel = new DashboardViewModel
            {
                KpiData = GetKpiData(allowedWarehouses),
                Warehouses = GetWarehouseFilters(allowedWarehouses),
                RecentAlerts = GetRecentAlerts(allowedWarehouses)
            };

            return viewModel;
        }

        public KpiDataViewModel GetKpiData(List<int> allowedWarehouses)
        {
            var stockQuery = _context.Stock
                .Include(s => s.Location)
                .Include(s => s.Item)
                .Where(s => allowedWarehouses.Contains(s.Location.WarehouseId));

            var totalItems = stockQuery.Select(s => s.ItemId).Distinct().Count();
            var totalLocations = _context.Locations
                .Where(l => allowedWarehouses.Contains(l.WarehouseId) && l.Active)
                .Count();

            var redLevelItems = stockQuery
                .Where(s => s.QtyOnHand <= 0)
                .Select(s => s.ItemId)
                .Distinct()
                .Count();

            var yellowLevelItems = stockQuery
                .Join(_context.ReplenishmentRules, s => s.ItemId, r => r.ItemId, (s, r) => new { s, r })
                .Where(sr => sr.s.QtyOnHand > 0 && sr.s.QtyOnHand < sr.r.Safety)
                .Select(sr => sr.s.ItemId)
                .Distinct()
                .Count();

            return new KpiDataViewModel
            {
                TotalItems = totalItems,
                TotalLocations = totalLocations,
                RedLevelItems = redLevelItems,
                YellowLevelItems = yellowLevelItems,
                GreenLevelItems = totalItems - redLevelItems - yellowLevelItems
            };
        }

        public TransactionChartDataViewModel GetTransactionChartData(List<int> allowedWarehouses, int days)
        {
            var startDate = DateTime.Today.AddDays(-days);
            
            var transactions = _context.Transactions
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .Where(t => t.Timestamp >= startDate &&
                           (allowedWarehouses.Contains(t.FromLocation.WarehouseId) ||
                            (t.ToLocation != null && allowedWarehouses.Contains(t.ToLocation.WarehouseId))))
                .GroupBy(t => new { Date = DbFunctions.TruncateTime(t.Timestamp), t.Type })
                .Select(g => new TransactionDataPoint
                {
                    Date = g.Key.Date.Value,
                    Type = g.Key.Type,
                    Count = g.Count(),
                    Quantity = g.Sum(t => t.Quantity)
                })
                .OrderBy(t => t.Date)
                .ToList();

            var dates = Enumerable.Range(0, days)
                .Select(i => startDate.AddDays(i))
                .ToList();

            var receiveData = dates.Select(d => transactions
                .Where(t => t.Date == d && t.Type == TransactionType.Receive)
                .Sum(t => t.Count))
                .ToList();

            var issueData = dates.Select(d => transactions
                .Where(t => t.Date == d && t.Type == TransactionType.Issue)
                .Sum(t => t.Count))
                .ToList();

            var moveData = dates.Select(d => transactions
                .Where(t => t.Date == d && t.Type == TransactionType.Move)
                .Sum(t => t.Count))
                .ToList();

            return new TransactionChartDataViewModel
            {
                Labels = dates.Select(d => d.ToString("MM/dd")).ToList(),
                ReceiveData = receiveData,
                IssueData = issueData,
                MoveData = moveData
            };
        }

        public TopItemsChartDataViewModel GetTopItemsChartData(List<int> allowedWarehouses, int top)
        {
            var topItems = _context.Transactions
                .Include(t => t.Item)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .Where(t => t.Type == TransactionType.Issue &&
                           t.Timestamp >= DateTime.Today.AddDays(-30) &&
                           (allowedWarehouses.Contains(t.FromLocation.WarehouseId) ||
                            (t.ToLocation != null && allowedWarehouses.Contains(t.ToLocation.WarehouseId))))
                .GroupBy(t => new { t.ItemId, t.Item.Code, t.Item.Name })
                .Select(g => new TopItemData
                {
                    ItemCode = g.Key.Code,
                    ItemName = g.Key.Name,
                    TotalQuantity = g.Sum(t => t.Quantity),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(i => i.TotalQuantity)
                .Take(top)
                .ToList();

            return new TopItemsChartDataViewModel
            {
                Labels = topItems.Select(i => i.ItemCode).ToList(),
                Data = topItems.Select(i => i.TotalQuantity).ToList(),
                ItemNames = topItems.Select(i => i.ItemName).ToList()
            };
        }

        public HeatmapDataViewModel GetHeatmapData(List<int> allowedWarehouses, int? warehouseId = null, string zone = null, string aisle = null)
        {
            var locationsQuery = _context.Locations
                .Include(l => l.Warehouse)
                .Where(l => allowedWarehouses.Contains(l.WarehouseId) && l.Active);

            if (warehouseId.HasValue)
                locationsQuery = locationsQuery.Where(l => l.WarehouseId == warehouseId.Value);

            if (!string.IsNullOrEmpty(zone))
                locationsQuery = locationsQuery.Where(l => l.Zone == zone);

            if (!string.IsNullOrEmpty(aisle))
                locationsQuery = locationsQuery.Where(l => l.Aisle == aisle);

            var locationData = locationsQuery
                .GroupJoin(_context.Stock, l => l.Id, s => s.LocationId, (l, stocks) => new
                {
                    Location = l,
                    TotalStock = stocks.Sum(s => (decimal?)s.QtyOnHand) ?? 0,
                    ItemCount = stocks.Count()
                })
                .ToList();

            var zones = locationData.Select(l => l.Location.Zone).Distinct().OrderBy(z => z).ToList();
            var aisles = locationData.Select(l => l.Location.Aisle).Distinct().OrderBy(a => a).ToList();

            var maxStock = locationData.Max(l => l.TotalStock);
            var heatmapData = new List<HeatmapPoint>();

            for (int zoneIndex = 0; zoneIndex < zones.Count; zoneIndex++)
            {
                for (int aisleIndex = 0; aisleIndex < aisles.Count; aisleIndex++)
                {
                    var zone_name = zones[zoneIndex];
                    var aisle_name = aisles[aisleIndex];
                    
                    var locationStock = locationData
                        .Where(l => l.Location.Zone == zone_name && l.Location.Aisle == aisle_name)
                        .Sum(l => l.TotalStock);

                    var locationCount = locationData
                        .Count(l => l.Location.Zone == zone_name && l.Location.Aisle == aisle_name);

                    if (locationCount > 0)
                    {
                        var intensity = maxStock > 0 ? (double)(locationStock / maxStock) : 0;
                        
                        heatmapData.Add(new HeatmapPoint
                        {
                            X = aisleIndex,
                            Y = zoneIndex,
                            V = intensity,
                            Zone = zone_name,
                            Aisle = aisle_name,
                            TotalStock = locationStock,
                            LocationCount = locationCount
                        });
                    }
                }
            }

            return new HeatmapDataViewModel
            {
                Data = heatmapData,
                ZoneLabels = zones,
                AisleLabels = aisles,
                MaxValue = maxStock
            };
        }

        public HeatmapFiltersViewModel GetHeatmapFilters(List<int> allowedWarehouses)
        {
            var warehouses = _context.Warehouses
                .Where(w => allowedWarehouses.Contains(w.Id) && w.Active)
                .Select(w => new { w.Id, w.Name })
                .ToList();

            var zones = _context.Locations
                .Where(l => allowedWarehouses.Contains(l.WarehouseId) && l.Active)
                .Select(l => l.Zone)
                .Distinct()
                .OrderBy(z => z)
                .ToList();

            var aisles = _context.Locations
                .Where(l => allowedWarehouses.Contains(l.WarehouseId) && l.Active)
                .Select(l => l.Aisle)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            return new HeatmapFiltersViewModel
            {
                Warehouses = warehouses.ToDictionary(w => w.Id, w => w.Name),
                Zones = zones,
                Aisles = aisles
            };
        }

        public List<AlertViewModel> GetRecentAlerts(List<int> allowedWarehouses)
        {
            var alerts = new List<AlertViewModel>();

            // Low stock alerts
            var lowStockItems = _context.Stock
                .Include(s => s.Item)
                .Include(s => s.Location.Warehouse)
                .Join(_context.ReplenishmentRules, s => s.ItemId, r => r.ItemId, (s, r) => new { s, r })
                .Where(sr => allowedWarehouses.Contains(sr.s.Location.WarehouseId) &&
                            sr.s.QtyOnHand <= sr.r.Safety)
                .GroupBy(sr => new { sr.s.ItemId, sr.s.Item.Code, sr.s.Item.Name })
                .Select(g => new
                {
                    ItemCode = g.Key.Code,
                    ItemName = g.Key.Name,
                    TotalStock = g.Sum(sr => sr.s.QtyOnHand),
                    SafetyLevel = g.Min(sr => sr.r.Safety)
                })
                .Take(10)
                .ToList();

            foreach (var item in lowStockItems)
            {
                var alertType = item.TotalStock <= 0 ? "danger" : "warning";
                var message = item.TotalStock <= 0 
                    ? $"Out of stock: {item.ItemCode}"
                    : $"Low stock: {item.ItemCode} ({item.TotalStock} remaining, safety level: {item.SafetyLevel})";

                alerts.Add(new AlertViewModel
                {
                    Type = alertType,
                    Message = message,
                    Timestamp = DateTime.Now
                });
            }

            return alerts.OrderByDescending(a => a.Timestamp).ToList();
        }

        private List<KeyValuePair<int, string>> GetWarehouseFilters(List<int> allowedWarehouses)
        {
            return _context.Warehouses
                .Where(w => allowedWarehouses.Contains(w.Id) && w.Active)
                .Select(w => new KeyValuePair<int, string>(w.Id, w.Name))
                .ToList();
        }

        public void Dispose()
        {
            // Context is managed by controller
        }
    }

    public class TransactionDataPoint
    {
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public int Count { get; set; }
        public decimal Quantity { get; set; }
    }

    public class TopItemData
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal TotalQuantity { get; set; }
        public int TransactionCount { get; set; }
    }
}
