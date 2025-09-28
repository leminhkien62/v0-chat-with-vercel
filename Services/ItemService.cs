using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WmsSystem.Data;
using WmsSystem.Models;

namespace WmsSystem.Services
{
    public class ItemService
    {
        private readonly WmsDbContext _context;

        public ItemService(WmsDbContext context)
        {
            _context = context;
        }

        public async Task<Item> CreateItemAsync(Item item)
        {
            // Check for duplicate code
            var existingItem = await _context.Items
                .FirstOrDefaultAsync(i => i.Code == item.Code);
                
            if (existingItem != null)
                throw new InvalidOperationException($"Item with code '{item.Code}' already exists.");

            item.CreatedAt = DateTime.Now;
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            
            return item;
        }

        public async Task<Item> UpdateItemAsync(Item item)
        {
            var existingItem = await _context.Items.FindAsync(item.Id);
            if (existingItem == null)
                throw new InvalidOperationException("Item not found.");

            // Check for duplicate code (excluding current item)
            var duplicateItem = await _context.Items
                .FirstOrDefaultAsync(i => i.Code == item.Code && i.Id != item.Id);
                
            if (duplicateItem != null)
                throw new InvalidOperationException($"Item with code '{item.Code}' already exists.");

            existingItem.Code = item.Code;
            existingItem.Name = item.Name;
            existingItem.UoM = item.UoM;
            existingItem.IsConsumable = item.IsConsumable;
            existingItem.Category = item.Category;
            existingItem.ImageUrl = item.ImageUrl;
            existingItem.Active = item.Active;
            
            await _context.SaveChangesAsync();
            return existingItem;
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.Stocks)
                .Include(i => i.PoLines)
                .Include(i => i.LpnItems)
                .Include(i => i.Requests)
                .FirstOrDefaultAsync(i => i.Id == itemId);
                
            if (item == null)
                throw new InvalidOperationException("Item not found.");

            // Check if item has references
            var hasReferences = item.Stocks.Any() || 
                               item.PoLines.Any() || 
                               item.LpnItems.Any() || 
                               item.Requests.Any();

            if (hasReferences)
                throw new InvalidOperationException("Cannot delete item that is referenced by other records. Please set it as inactive instead.");

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
