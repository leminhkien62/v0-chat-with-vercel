using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WmsSystem.Models
{
    public class Item
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Code { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [StringLength(10)]
        public string UoM { get; set; } = "EA";
        
        public bool IsConsumable { get; set; } = true;
        
        [StringLength(50)]
        public string Category { get; set; }
        
        [StringLength(500)]
        public string ImageUrl { get; set; }
        
        public bool Active { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
        public virtual ICollection<PoLine> PoLines { get; set; } = new List<PoLine>();
        public virtual ICollection<LpnItem> LpnItems { get; set; } = new List<LpnItem>();
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
        public virtual ICollection<ReplenishmentRule> ReplenishmentRules { get; set; } = new List<ReplenishmentRule>();
    }
}
