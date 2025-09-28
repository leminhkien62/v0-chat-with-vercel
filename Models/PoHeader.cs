using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WmsSystem.Models
{
    public class PoHeader
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PoNo { get; set; }
        
        public DateTime PoDate { get; set; }
        
        [StringLength(200)]
        public string Supplier { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public virtual ICollection<PoLine> PoLines { get; set; } = new List<PoLine>();
    }
}
