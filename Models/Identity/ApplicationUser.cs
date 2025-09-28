using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WmsSystem.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string FullName { get; set; }
        
        public int? DeptId { get; set; }
        
        public virtual Department Department { get; set; }
        public virtual ICollection<UserWarehouse> UserWarehouses { get; set; } = new List<UserWarehouse>();
    }
}
