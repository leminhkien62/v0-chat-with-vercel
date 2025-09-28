using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class ReplenishmentRule
    {
        public int Id { get; set; }
        
        public int ItemId { get; set; }
        public int WarehouseId { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Safety { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Max { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal MOQ { get; set; }
        
        public int LeadTimeDays { get; set; }
        
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        
        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }
    }
}
