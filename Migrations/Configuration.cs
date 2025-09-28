using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity.Migrations;
using WmsSystem.Data;
using WmsSystem.Models;
using WmsSystem.Models.Identity;

namespace WmsSystem.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<WmsDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(WmsDbContext context)
        {
            // Create roles
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var roles = new[] { "Admin", "Store", "Manager", "Viewer" };
            
            foreach (var roleName in roles)
            {
                if (!roleManager.RoleExists(roleName))
                {
                    roleManager.Create(new IdentityRole(roleName));
                }
            }

            // Create default admin user
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            
            if (userManager.FindByEmail("admin@wms.com") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@wms.com",
                    Email = "admin@wms.com",
                    FullName = "System Administrator"
                };

                var result = userManager.Create(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    userManager.AddToRole(adminUser.Id, "Admin");
                }
            }

            // Create default departments
            context.Departments.AddOrUpdate(d => d.DeptId,
                new Department { DeptId = "IT", Name = "Information Technology", Active = true },
                new Department { DeptId = "HR", Name = "Human Resources", Active = true },
                new Department { DeptId = "FIN", Name = "Finance", Active = true },
                new Department { DeptId = "OPS", Name = "Operations", Active = true }
            );

            // Create sample warehouses
            context.Warehouses.AddOrUpdate(w => w.Code,
                new Warehouse { Code = "WH01", Name = "Main Warehouse", Active = true },
                new Warehouse { Code = "WH02", Name = "Secondary Warehouse", Active = true }
            );

            context.SaveChanges();
        }
    }
}
