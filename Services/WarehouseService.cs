using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WmsSystem.Data;
using WmsSystem.Models;

namespace WmsSystem.Services
{
    public class WarehouseService
    {
        private readonly WmsDbContext _context;

        public WarehouseService(WmsDbContext context)
        {
            _context = context;
        }

        public async Task<Warehouse> CreateWarehouseAsync(Warehouse warehouse)
        {
            // Check for duplicate code
            var existingWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Code == warehouse.Code);
                
            if (existingWarehouse != null)
                throw new InvalidOperationException($"Warehouse with code '{warehouse.Code}' already exists.");

            warehouse.CreatedAt = DateTime.Now;
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();
            
            return warehouse;
        }

        public async Task<Warehouse> UpdateWarehouseAsync(Warehouse warehouse)
        {
            var existingWarehouse = await _context.Warehouses.FindAsync(warehouse.Id);
            if (existingWarehouse == null)
                throw new InvalidOperationException("Warehouse not found.");

            // Check for duplicate code (excluding current warehouse)
            var duplicateWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Code == warehouse.Code && w.Id != warehouse.Id);
                
            if (duplicateWarehouse != null)
                throw new InvalidOperationException($"Warehouse with code '{warehouse.Code}' already exists.");

            existingWarehouse.Code = warehouse.Code;
            existingWarehouse.Name = warehouse.Name;
            existingWarehouse.Active = warehouse.Active;
            
            await _context.SaveChangesAsync();
            return existingWarehouse;
        }

        public async Task DeleteWarehouseAsync(int warehouseId)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.Locations)
                .FirstOrDefaultAsync(w => w.Id == warehouseId);
                
            if (warehouse == null)
                throw new InvalidOperationException("Warehouse not found.");

            // Check if warehouse has locations
            if (warehouse.Locations.Any())
                throw new InvalidOperationException("Cannot delete warehouse that contains locations. Please remove all locations first.");

            // Check if warehouse is referenced in other tables
            var hasReferences = await _context.DepartmentWarehouses.AnyAsync(dw => dw.WarehouseId == warehouseId) ||
                               await _context.UserWarehouses.AnyAsync(uw => uw.WarehouseId == warehouseId) ||
                               await _context.ReplenishmentRules.AnyAsync(rr => rr.WarehouseId == warehouseId);

            if (hasReferences)
                throw new InvalidOperationException("Cannot delete warehouse that is referenced by other records. Please set it as inactive instead.");

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();
        }
    }
}
