using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class PoLine
    {
        public int Id { get; set; }
        
        public int PoHeaderId { get; set; }
        public int ItemId { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal QtyOrdered { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal QtyReceived { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }
        
        [ForeignKey("PoHeaderId")]
        public virtual PoHeader PoHeader { get; set; }
        
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
    }
}
