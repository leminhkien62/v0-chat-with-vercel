using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using WmsSystem.Models;

namespace WmsSystem.ViewModels
{
    public class ReceivingIndexViewModel
    {
        public List<PoHeader> PendingPos { get; set; } = new List<PoHeader>();
        public List<Transaction> RecentReceipts { get; set; } = new List<Transaction>();
    }

    public class ReceivePoViewModel
    {
        public PoHeader Po { get; set; }
        public List<ReceiveLineViewModel> ReceiveLines { get; set; } = new List<ReceiveLineViewModel>();
        
        [Required]
        [Display(Name = "Receive Location")]
        public int SelectedLocationId { get; set; }
        
        public SelectList Locations { get; set; }
    }

    public class ReceiveLineViewModel
    {
        public int PoLineId { get; set; }
        public Item Item { get; set; }
        public decimal QtyOrdered { get; set; }
        public decimal QtyReceived { get; set; }
        
        [Display(Name = "Qty to Receive")]
        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be positive")]
        public decimal QtyToReceive { get; set; }
        
        [Display(Name = "Lot Number")]
        [StringLength(50)]
        public string Lot { get; set; }
        
        [Display(Name = "Serial Number")]
        [StringLength(50)]
        public string Serial { get; set; }
        
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }
    }

    public class PrintLpnViewModel
    {
        public Lpn Lpn { get; set; }
        public string QrCodeData { get; set; }
        public string QrCodeImage { get; set; }
    }

    public class PutawayLpnViewModel
    {
        public Lpn Lpn { get; set; }
        public int CurrentLocationId { get; set; }
        
        [Required]
        [Display(Name = "Target Location")]
        public int TargetLocationId { get; set; }
        
        public SelectList Locations { get; set; }
    }
}
