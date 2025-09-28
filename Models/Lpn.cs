using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class Lpn
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string LpnCode { get; set; }
        
        public int LocationId { get; set; }
        
        public bool Closed { get; set; } = false;
        
        [StringLength(500)]
        public string Note { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [ForeignKey("LocationId")]
        public virtual Location Location { get; set; }
        
        public virtual ICollection<LpnItem> LpnItems { get; set; } = new List<LpnItem>();
    }
}
