using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WmsSystem.Models
{
    public class Department
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string DeptId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public bool Active { get; set; } = true;
        
        public virtual ICollection<DepartmentWarehouse> DepartmentWarehouses { get; set; } = new List<DepartmentWarehouse>();
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}
