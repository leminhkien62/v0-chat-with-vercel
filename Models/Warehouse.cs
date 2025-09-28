using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class Warehouse
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Code { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public bool Active { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
        public virtual ICollection<DepartmentWarehouse> DepartmentWarehouses { get; set; } = new List<DepartmentWarehouse>();
        public virtual ICollection<UserWarehouse> UserWarehouses { get; set; } = new List<UserWarehouse>();
    }
}
