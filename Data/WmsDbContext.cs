using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using WmsSystem.Models;
using WmsSystem.Models.Identity;

namespace WmsSystem.Data
{
    public class WmsDbContext : IdentityDbContext<ApplicationUser>
    {
        public WmsDbContext() : base("DefaultConnection")
        {
        }
        
        public static WmsDbContext Create()
        {
            return new WmsDbContext();
        }
        
        // Master Data
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Department> Departments { get; set; }
        
        // Inventory
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        
        // Purchase Orders
        public DbSet<PoHeader> PoHeaders { get; set; }
        public DbSet<PoLine> PoLines { get; set; }
        
        // LPN Management
        public DbSet<Lpn> Lpns { get; set; }
        public DbSet<LpnItem> LpnItems { get; set; }
        
        // Requests
        public DbSet<Request> Requests { get; set; }
        
        // Rules
        public DbSet<ReplenishmentRule> ReplenishmentRules { get; set; }
        
        // Security
        public DbSet<DepartmentWarehouse> DepartmentWarehouses { get; set; }
        public DbSet<UserWarehouse> UserWarehouses { get; set; }
        
        // Audit
        public DbSet<SystemLog> SystemLogs { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure unique constraints
            modelBuilder.Entity<Warehouse>()
                .HasIndex(w => w.Code)
                .IsUnique();
                
            modelBuilder.Entity<Location>()
                .HasIndex(l => l.Code)
                .IsUnique();
                
            modelBuilder.Entity<Item>()
                .HasIndex(i => i.Code)
                .IsUnique();
                
            modelBuilder.Entity<PoHeader>()
                .HasIndex(p => p.PoNo)
                .IsUnique();
                
            modelBuilder.Entity<Lpn>()
                .HasIndex(l => l.LpnCode)
                .IsUnique();
                
            // Configure cascade deletes
            modelBuilder.Entity<Location>()
                .HasRequired(l => l.Warehouse)
                .WithMany(w => w.Locations)
                .HasForeignKey(l => l.WarehouseId)
                .WillCascadeOnDelete(false);
                
            modelBuilder.Entity<Stock>()
                .HasRequired(s => s.Item)
                .WithMany(i => i.Stocks)
                .HasForeignKey(s => s.ItemId)
                .WillCascadeOnDelete(false);
                
            modelBuilder.Entity<Stock>()
                .HasRequired(s => s.Location)
                .WithMany(l => l.Stocks)
                .HasForeignKey(s => s.LocationId)
                .WillCascadeOnDelete(false);
        }
    }
}
