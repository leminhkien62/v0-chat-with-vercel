using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WmsSystem.Models.Identity;

namespace WmsSystem.Models
{
    public class UserWarehouse
    {
        public int Id { get; set; }
        
        [StringLength(128)]
        public string UserId { get; set; }
        
        public int WarehouseId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        
        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }
    }
}
