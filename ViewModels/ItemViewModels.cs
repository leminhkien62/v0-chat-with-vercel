using System.Collections.Generic;
using WmsSystem.Models;

namespace WmsSystem.ViewModels
{
    public class ItemIndexViewModel
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public List<string> Categories { get; set; } = new List<string>();
        public string SelectedCategory { get; set; }
        public bool? SelectedIsConsumable { get; set; }
    }
}
