using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class Location
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Code { get; set; }
        
        public int WarehouseId { get; set; }
        
        [StringLength(20)]
        public string Zone { get; set; }
        
        [StringLength(20)]
        public string Aisle { get; set; }
        
        [StringLength(20)]
        public string Rack { get; set; }
        
        [StringLength(20)]
        public string Bin { get; set; }
        
        public bool Locked { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }
        
        public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
        public virtual ICollection<Lpn> Lpns { get; set; } = new List<Lpn>();
    }
}
