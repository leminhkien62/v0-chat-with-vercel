using System;
using System.Collections.Generic;

namespace WmsSystem.ViewModels
{
    public class DashboardViewModel
    {
        public KpiDataViewModel KpiData { get; set; }
        public List<KeyValuePair<int, string>> Warehouses { get; set; }
        public List<AlertViewModel> RecentAlerts { get; set; }
    }

    public class KpiDataViewModel
    {
        public int TotalItems { get; set; }
        public int TotalLocations { get; set; }
        public int RedLevelItems { get; set; }
        public int YellowLevelItems { get; set; }
        public int GreenLevelItems { get; set; }
    }

    public class TransactionChartDataViewModel
    {
        public List<string> Labels { get; set; }
        public List<int> ReceiveData { get; set; }
        public List<int> IssueData { get; set; }
        public List<int> MoveData { get; set; }
    }

    public class TopItemsChartDataViewModel
    {
        public List<string> Labels { get; set; }
        public List<decimal> Data { get; set; }
        public List<string> ItemNames { get; set; }
    }

    public class HeatmapDataViewModel
    {
        public List<HeatmapPoint> Data { get; set; }
        public List<string> ZoneLabels { get; set; }
        public List<string> AisleLabels { get; set; }
        public decimal MaxValue { get; set; }
    }

    public class HeatmapPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double V { get; set; }
        public string Zone { get; set; }
        public string Aisle { get; set; }
        public decimal TotalStock { get; set; }
        public int LocationCount { get; set; }
    }

    public class HeatmapFiltersViewModel
    {
        public Dictionary<int, string> Warehouses { get; set; }
        public List<string> Zones { get; set; }
        public List<string> Aisles { get; set; }
    }

    public class AlertViewModel
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
