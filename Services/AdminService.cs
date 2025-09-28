using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WmsSystem.Data;
using WmsSystem.Models;
using WmsSystem.Models.Identity;
using WmsSystem.ViewModels;

namespace WmsSystem.Services
{
    public class AdminService
    {
        private readonly WmsDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(WmsDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task UpdateUserAsync(EditUserViewModel viewModel)
        {
            var user = await _context.Users
                .Include(u => u.UserWarehouses)
                .FirstOrDefaultAsync(u => u.Id == viewModel.User.Id);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            // Update user properties
            user.FullName = viewModel.User.FullName;
            user.DeptId = viewModel.User.DeptId;
            user.Email = viewModel.User.Email;
            user.UserName = viewModel.User.Email;

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user.Id);
            var rolesToRemove = currentRoles.Except(viewModel.SelectedRoles ?? new List<string>()).ToList();
            var rolesToAdd = (viewModel.SelectedRoles ?? new List<string>()).Except(currentRoles).ToList();

            if (rolesToRemove.Any())
                await _userManager.RemoveFromRolesAsync(user.Id, rolesToRemove.ToArray());

            if (rolesToAdd.Any())
                await _userManager.AddToRolesAsync(user.Id, rolesToAdd.ToArray());

            // Update warehouse assignments
            var currentWarehouseIds = user.UserWarehouses.Select(uw => uw.WarehouseId).ToList();
            var warehousesToRemove = currentWarehouseIds.Except(viewModel.SelectedWarehouseIds ?? new List<int>()).ToList();
            var warehousesToAdd = (viewModel.SelectedWarehouseIds ?? new List<int>()).Except(currentWarehouseIds).ToList();

            // Remove warehouse assignments
            var userWarehousesToRemove = user.UserWarehouses.Where(uw => warehousesToRemove.Contains(uw.WarehouseId)).ToList();
            foreach (var userWarehouse in userWarehousesToRemove)
            {
                _context.UserWarehouses.Remove(userWarehouse);
            }

            // Add warehouse assignments
            foreach (var warehouseId in warehousesToAdd)
            {
                _context.UserWarehouses.Add(new UserWarehouse
                {
                    UserId = user.Id,
                    WarehouseId = warehouseId
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Department> CreateDepartmentAsync(Department department)
        {
            // Check for duplicate DeptId
            var existingDept = await _context.Departments
                .FirstOrDefaultAsync(d => d.DeptId == department.DeptId);

            if (existingDept != null)
                throw new InvalidOperationException($"Department with ID '{department.DeptId}' already exists.");

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return department;
        }

        public async Task SaveDepartmentWarehouseMappingAsync(int deptId, List<int> warehouseIds)
        {
            // Remove existing mappings
            var existingMappings = await _context.DepartmentWarehouses
                .Where(dw => dw.DeptId == deptId)
                .ToListAsync();

            _context.DepartmentWarehouses.RemoveRange(existingMappings);

            // Add new mappings
            foreach (var warehouseId in warehouseIds)
            {
                _context.DepartmentWarehouses.Add(new DepartmentWarehouse
                {
                    DeptId = deptId,
                    WarehouseId = warehouseId
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
