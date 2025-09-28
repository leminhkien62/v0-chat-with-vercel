using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WmsSystem.Models;

namespace WmsSystem.ViewModels
{
    public class IssueIndexViewModel
    {
        public List<Request> PendingRequests { get; set; } = new List<Request>();
        public List<Transaction> RecentIssues { get; set; } = new List<Transaction>();
    }

    public class CreateIssueViewModel
    {
        [Required]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Qty { get; set; }

        [Display(Name = "Reference Number")]
        [StringLength(100)]
        public string RefNo { get; set; }

        [Display(Name = "Note")]
        [StringLength(500)]
        public string Note { get; set; }
    }

    public class ProcessRequestViewModel
    {
        public Request Request { get; set; }
        public List<PickListItemViewModel> PickList { get; set; } = new List<PickListItemViewModel>();
        public decimal TotalAvailable { get; set; }
    }

    public class PickListItemViewModel
    {
        public int StockId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string LocationCode { get; set; }
        public string WarehouseCode { get; set; }
        public string Lot { get; set; }
        public string Serial { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal SuggestedQty { get; set; }
        
        [Display(Name = "Qty to Pick")]
        [Range(0, double.MaxValue)]
        public decimal QtyToPick { get; set; }
        
        public int Priority { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    public class PickListViewModel
    {
        public Item Item { get; set; }
        public decimal RequestedQty { get; set; }
        public List<PickListItemViewModel> PickList { get; set; } = new List<PickListItemViewModel>();
        public decimal TotalAvailable { get; set; }
    }

    public class MoveIndexViewModel
    {
        public List<Transaction> RecentMoves { get; set; } = new List<Transaction>();
    }

    public class CreateMoveViewModel
    {
        [Required]
        [Display(Name = "Stock to Move")]
        public int StockId { get; set; }

        [Required]
        [Display(Name = "Target Location")]
        public int ToLocationId { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Qty { get; set; }

        [Display(Name = "Reference Number")]
        [StringLength(100)]
        public string RefNo { get; set; }

        [Display(Name = "Note")]
        [StringLength(500)]
        public string Note { get; set; }
    }

    public class RequestLineViewModel
    {
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
    }

    public class PickingAnalysisViewModel
    {
        public int ItemId { get; set; }
        public decimal RequestedQty { get; set; }
        public decimal TotalAvailable { get; set; }
        public int LocationsRequired { get; set; }
        public int WarehousesInvolved { get; set; }
        public int ExpiringSoonCount { get; set; }
        public bool CanFulfillCompletely { get; set; }
        public string PickingComplexity { get; set; }
    }
}
