using System.Collections.Generic;
using System.Web.Mvc;
using WmsSystem.Models;

namespace WmsSystem.ViewModels
{
    public class LocationIndexViewModel
    {
        public List<Location> Locations { get; set; } = new List<Location>();
        public List<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public int? SelectedWarehouseId { get; set; }
    }

    public class LocationCreateViewModel
    {
        public Location Location { get; set; } = new Location();
        public SelectList Warehouses { get; set; }
    }

    public class LocationEditViewModel
    {
        public Location Location { get; set; }
        public SelectList Warehouses { get; set; }
    }
}
