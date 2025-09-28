using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WmsSystem.Data;
using WmsSystem.Models;
using WmsSystem.Models.Identity;
using WmsSystem.Services;
using WmsSystem.ViewModels;

namespace WmsSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly WmsDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AdminService _adminService;

        public AdminController()
        {
            _context = new WmsDbContext();
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(_context));
            _roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_context));
            _adminService = new AdminService(_context, _userManager);
        }

        public ActionResult Index()
        {
            return View();
        }

        // User Management
        public async Task<ActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserWarehouses.Select(uw => uw.Warehouse))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var viewModel = new UserManagementViewModel();
            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user.Id);
                viewModel.Users.Add(new UserViewModel
                {
                    User = user,
                    Roles = roles.ToList(),
                    Warehouses = user.UserWarehouses.Select(uw => uw.Warehouse).ToList()
                });
            }

            return View(viewModel);
        }

        public async Task<ActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserWarehouses)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return HttpNotFound();

            var viewModel = new EditUserViewModel
            {
                User = user,
                SelectedRoles = (await _userManager.GetRolesAsync(user.Id)).ToList(),
                SelectedWarehouseIds = user.UserWarehouses.Select(uw => uw.WarehouseId).ToList()
            };

            await PopulateEditUserDropdowns(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditUser(EditUserViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _adminService.UpdateUserAsync(viewModel);
                    TempData["Success"] = "User updated successfully.";
                    return RedirectToAction("Users");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            await PopulateEditUserDropdowns(viewModel);
            return View(viewModel);
        }

        // Department Management
        public async Task<ActionResult> Departments()
        {
            var departments = await _context.Departments
                .Include(d => d.DepartmentWarehouses.Select(dw => dw.Warehouse))
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(departments);
        }

        public ActionResult CreateDepartment()
        {
            return View(new Department());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDepartment(Department department)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _adminService.CreateDepartmentAsync(department);
                    TempData["Success"] = "Department created successfully.";
                    return RedirectToAction("Departments");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(department);
        }

        public async Task<ActionResult> DepartmentWarehouseMapping(int? deptId)
        {
            var viewModel = new DepartmentWarehouseMappingViewModel();
            
            viewModel.Departments = await _context.Departments
                .Where(d => d.Active)
                .OrderBy(d => d.Name)
                .ToListAsync();

            viewModel.Warehouses = await _context.Warehouses
                .Where(w => w.Active)
                .OrderBy(w => w.Code)
                .ToListAsync();

            if (deptId.HasValue)
            {
                viewModel.SelectedDeptId = deptId.Value;
                viewModel.MappedWarehouseIds = await _context.DepartmentWarehouses
                    .Where(dw => dw.DeptId == deptId.Value)
                    .Select(dw => dw.WarehouseId)
                    .ToListAsync();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveDepartmentWarehouseMapping(DepartmentWarehouseMappingViewModel viewModel)
        {
            try
            {
                await _adminService.SaveDepartmentWarehouseMappingAsync(viewModel.SelectedDeptId, viewModel.SelectedWarehouseIds ?? new List<int>());
                TempData["Success"] = "Department-Warehouse mapping updated successfully.";
                return RedirectToAction("DepartmentWarehouseMapping", new { deptId = viewModel.SelectedDeptId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("DepartmentWarehouseMapping", new { deptId = viewModel.SelectedDeptId });
            }
        }

        // System Logs
        public async Task<ActionResult> SystemLogs(DateTime? fromDate, DateTime? toDate, string userName, string controller)
        {
            var viewModel = new SystemLogsViewModel
            {
                FromDate = fromDate ?? DateTime.Today.AddDays(-7),
                ToDate = toDate ?? DateTime.Today.AddDays(1),
                UserName = userName,
                Controller = controller
            };

            var query = _context.SystemLogs.AsQueryable();

            if (viewModel.FromDate.HasValue)
                query = query.Where(l => l.Ts >= viewModel.FromDate.Value);

            if (viewModel.ToDate.HasValue)
                query = query.Where(l => l.Ts <= viewModel.ToDate.Value);

            if (!string.IsNullOrEmpty(viewModel.UserName))
                query = query.Where(l => l.UserName.Contains(viewModel.UserName));

            if (!string.IsNullOrEmpty(viewModel.Controller))
                query = query.Where(l => l.Controller.Contains(viewModel.Controller));

            viewModel.Logs = await query
                .OrderByDescending(l => l.Ts)
                .Take(1000)
                .ToListAsync();

            return View(viewModel);
        }

        private async Task PopulateEditUserDropdowns(EditUserViewModel viewModel)
        {
            var departments = await _context.Departments
                .Where(d => d.Active)
                .OrderBy(d => d.Name)
                .ToListAsync();
            viewModel.Departments = new SelectList(departments, "Id", "Name");

            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            viewModel.AllRoles = roles.Select(r => r.Name).ToList();

            var warehouses = await _context.Warehouses
                .Where(w => w.Active)
                .OrderBy(w => w.Code)
                .ToListAsync();
            viewModel.AllWarehouses = warehouses;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _userManager?.Dispose();
                _roleManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
