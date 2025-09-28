using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class LpnItem
    {
        public int Id { get; set; }
        
        public int LpnId { get; set; }
        public int ItemId { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Qty { get; set; }
        
        [StringLength(50)]
        public string Lot { get; set; }
        
        [StringLength(50)]
        public string Serial { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        
        [ForeignKey("LpnId")]
        public virtual Lpn Lpn { get; set; }
        
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
    }
}
