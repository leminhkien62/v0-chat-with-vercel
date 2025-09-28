using System.ComponentModel.DataAnnotations.Schema;

namespace WmsSystem.Models
{
    public class DepartmentWarehouse
    {
        public int Id { get; set; }
        
        public int DeptId { get; set; }
        public int WarehouseId { get; set; }
        
        [ForeignKey("DeptId")]
        public virtual Department Department { get; set; }
        
        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }
    }
}
