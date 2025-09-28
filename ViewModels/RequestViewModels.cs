using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using WmsSystem.Models;

namespace WmsSystem.ViewModels
{
    public class CreateRequestViewModel
    {
        [Required]
        [Display(Name = "Department")]
        public int DeptId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Requester Name")]
        public string Requester { get; set; }

        [Required]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        [Required]
        [Range(0.01, 999999)]
        [Display(Name = "Quantity")]
        public decimal Qty { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string Note { get; set; }

        public List<SelectListItem> Departments { get; set; }
        public List<SelectListItem> Items { get; set; }
    }

    public class RequestListViewModel
    {
        public int Id { get; set; }
        public string Requester { get; set; }
        public string DepartmentName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public string UoM { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Note { get; set; }
    }

    public class ProcessRequestViewModel
    {
        public Request Request { get; set; }
        public List<PickListItemViewModel> PickList { get; set; }
        public decimal TotalAvailable { get; set; }
        public string Notes { get; set; }
    }

    public class PickListItemViewModel
    {
        public int StockId { get; set; }
        public string LocationCode { get; set; }
        public string WarehouseCode { get; set; }
        public string Lot { get; set; }
        public string Serial { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal SuggestedQty { get; set; }
        public decimal PickQty { get; set; }
        public int Priority { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    public class ItemAvailabilityViewModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UoM { get; set; }
        public decimal TotalAvailable { get; set; }
        public List<StockLocationInfo> Locations { get; set; }
    }
}
