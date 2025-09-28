using System;
using System.Collections.Generic;
using System.Web.Mvc;
using WmsSystem.Models;
using WmsSystem.Models.Identity;

namespace WmsSystem.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
    }

    public class UserViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    }

    public class EditUserViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> SelectedRoles { get; set; } = new List<string>();
        public List<int> SelectedWarehouseIds { get; set; } = new List<int>();
        
        // Dropdown data
        public SelectList Departments { get; set; }
        public List<string> AllRoles { get; set; } = new List<string>();
        public List<Warehouse> AllWarehouses { get; set; } = new List<Warehouse>();
    }

    public class DepartmentWarehouseMappingViewModel
    {
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public int SelectedDeptId { get; set; }
        public List<int> SelectedWarehouseIds { get; set; } = new List<int>();
        public List<int> MappedWarehouseIds { get; set; } = new List<int>();
    }

    public class SystemLogsViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string UserName { get; set; }
        public string Controller { get; set; }
        public List<SystemLog> Logs { get; set; } = new List<SystemLog>();
    }
}
