using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public enum RequestStatus
    {
        New,
        Processing,
        Completed,
        Rejected
    }
    
    public class Request
    {
        public int Id { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int DeptId { get; set; }
        
        [StringLength(100)]
        public string Requester { get; set; }
        
        public int ItemId { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Qty { get; set; }
        
        public RequestStatus Status { get; set; } = RequestStatus.New;
        
        [StringLength(500)]
        public string Note { get; set; }
        
        [StringLength(100)]
        public string ProcessedBy { get; set; }
        
        public DateTime? ProcessedAt { get; set; }
        
        [ForeignKey("DeptId")]
        public virtual Department Department { get; set; }
        
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
    }
}
