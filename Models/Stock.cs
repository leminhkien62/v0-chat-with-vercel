using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class Stock
    {
        public int Id { get; set; }
        
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal QtyOnHand { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal QtyAllocated { get; set; }
        
        [StringLength(50)]
        public string Lot { get; set; }
        
        [StringLength(50)]
        public string Serial { get; set; }
        
        [StringLength(50)]
        public string Lpn { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        
        [ForeignKey("LocationId")]
        public virtual Location Location { get; set; }
    }
}
