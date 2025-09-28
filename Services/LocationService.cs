using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WmsSystem.Data;
using WmsSystem.Models;

namespace WmsSystem.Services
{
    public class LocationService
    {
        private readonly WmsDbContext _context;

        public LocationService(WmsDbContext context)
        {
            _context = context;
        }

        public async Task<Location> CreateLocationAsync(Location location)
        {
            // Generate location code if not provided
            if (string.IsNullOrEmpty(location.Code))
            {
                location.Code = GenerateLocationCode(location);
            }

            // Check for duplicate code
            var existingLocation = await _context.Locations
                .FirstOrDefaultAsync(l => l.Code == location.Code);
                
            if (existingLocation != null)
                throw new InvalidOperationException($"Location with code '{location.Code}' already exists.");

            // Validate warehouse exists
            var warehouse = await _context.Warehouses.FindAsync(location.WarehouseId);
            if (warehouse == null)
                throw new InvalidOperationException("Selected warehouse does not exist.");

            location.CreatedAt = DateTime.Now;
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();
            
            return location;
        }

        public async Task<Location> UpdateLocationAsync(Location location)
        {
            var existingLocation = await _context.Locations.FindAsync(location.Id);
            if (existingLocation == null)
                throw new InvalidOperationException("Location not found.");

            // Check for duplicate code (excluding current location)
            var duplicateLocation = await _context.Locations
                .FirstOrDefaultAsync(l => l.Code == location.Code && l.Id != location.Id);
                
            if (duplicateLocation != null)
                throw new InvalidOperationException($"Location with code '{location.Code}' already exists.");

            existingLocation.Code = location.Code;
            existingLocation.WarehouseId = location.WarehouseId;
            existingLocation.Zone = location.Zone;
            existingLocation.Aisle = location.Aisle;
            existingLocation.Rack = location.Rack;
            existingLocation.Bin = location.Bin;
            existingLocation.Locked = location.Locked;
            
            await _context.SaveChangesAsync();
            return existingLocation;
        }

        public async Task DeleteLocationAsync(int locationId)
        {
            var location = await _context.Locations
                .Include(l => l.Stocks)
                .Include(l => l.Lpns)
                .FirstOrDefaultAsync(l => l.Id == locationId);
                
            if (location == null)
                throw new InvalidOperationException("Location not found.");

            // Check if location has stock
            if (location.Stocks.Any(s => s.QtyOnHand > 0))
                throw new InvalidOperationException("Cannot delete location that contains stock.");

            // Check if location has LPNs
            if (location.Lpns.Any())
                throw new InvalidOperationException("Cannot delete location that contains LPNs.");

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
        }

        public async Task ToggleLockAsync(int locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null)
                throw new InvalidOperationException("Location not found.");

            location.Locked = !location.Locked;
            await _context.SaveChangesAsync();
        }

        private string GenerateLocationCode(Location location)
        {
            var warehouse = _context.Warehouses.Find(location.WarehouseId);
            var warehouseCode = warehouse?.Code ?? "WH";
            
            return $"{warehouseCode}-{location.Zone ?? "ZZ"}-{location.Aisle ?? "AA"}-{location.Rack ?? "RR"}-{location.Bin ?? "BB"}";
        }
    }
}
