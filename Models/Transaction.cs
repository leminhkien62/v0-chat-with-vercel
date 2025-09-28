using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public enum TransactionType
    {
        Receive,
        Issue,
        Move,
        CountAdjust
    }
    
    public class Transaction
    {
        public int Id { get; set; }
        
        public DateTime Ts { get; set; } = DateTime.Now;
        
        public TransactionType Type { get; set; }
        
        public int ItemId { get; set; }
        
        public int? FromLocationId { get; set; }
        public int? ToLocationId { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Qty { get; set; }
        
        [StringLength(100)]
        public string RefNo { get; set; }
        
        [StringLength(100)]
        public string UserName { get; set; }
        
        [StringLength(500)]
        public string Note { get; set; }
        
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        
        [ForeignKey("FromLocationId")]
        public virtual Location FromLocation { get; set; }
        
        [ForeignKey("ToLocationId")]
        public virtual Location ToLocation { get; set; }
    }
}
