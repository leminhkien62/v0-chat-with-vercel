using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WmsSystem.Data;
using WmsSystem.Models;

namespace WmsSystem.Services
{
    public class ErpIntegrationService
    {
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly int _timeout;
        private readonly WmsDbContext _context;

        public ErpIntegrationService()
        {
            _baseUrl = ConfigurationManager.AppSettings["ERP.BaseUrl"];
            _username = ConfigurationManager.AppSettings["ERP.Username"];
            _password = ConfigurationManager.AppSettings["ERP.Password"];
            _timeout = int.Parse(ConfigurationManager.AppSettings["ERP.Timeout"] ?? "30000");
            _context = new WmsDbContext();
        }

        public async Task<int> SyncPurchaseOrdersAsync(DateTime? fromDate = null)
        {
            var syncDate = fromDate ?? DateTime.Today.AddDays(-30);
            var syncedCount = 0;

            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(_timeout) })
                {
                    // Add authentication if configured
                    if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                    {
                        var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_username}:{_password}"));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
                    }

                    var url = $"{_baseUrl}/po?from={syncDate:yyyy-MM-dd}";
                    var response = await client.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        var erpPos = JsonConvert.DeserializeObject<List<ErpPurchaseOrder>>(jsonContent);

                        foreach (var erpPo in erpPos)
                        {
                            await ProcessErpPurchaseOrder(erpPo);
                            syncedCount++;
                        }
                    }
                    else
                    {
                        throw new Exception($"ERP API returned {response.StatusCode}: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error (implement logging as needed)
                throw new Exception($"Failed to sync purchase orders: {ex.Message}", ex);
            }

            return syncedCount;
        }

        public async Task SendReceiptToErpAsync(string poNo, string lpnCode, List<ReceiptLine> receiptLines)
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(_timeout) })
                {
                    if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                    {
                        var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_username}:{_password}"));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
                    }

                    var receiptData = new
                    {
                        PoNo = poNo,
                        LpnCode = lpnCode,
                        ReceiptDate = DateTime.Now,
                        Lines = receiptLines.Select(rl => new
                        {
                            ItemCode = rl.ItemCode,
                            QtyReceived = rl.QtyReceived,
                            Lot = rl.Lot,
                            Serial = rl.Serial
                        })
                    };

                    var json = JsonConvert.SerializeObject(receiptData);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{_baseUrl}/receipts", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"ERP API returned {response.StatusCode}: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the receiving process
                // Implement retry mechanism as needed
                throw new Exception($"Failed to send receipt to ERP: {ex.Message}", ex);
            }
        }

        private async Task ProcessErpPurchaseOrder(ErpPurchaseOrder erpPo)
        {
            // Check if PO already exists
            var existingPo = await _context.PoHeaders
                .Include(p => p.PoLines)
                .FirstOrDefaultAsync(p => p.PoNo == erpPo.PoNo);

            if (existingPo == null)
            {
                // Create new PO
                var poHeader = new PoHeader
                {
                    PoNo = erpPo.PoNo,
                    PoDate = erpPo.PoDate,
                    Supplier = erpPo.Supplier,
                    CreatedAt = DateTime.Now
                };

                _context.PoHeaders.Add(poHeader);
                await _context.SaveChangesAsync();

                // Add PO lines
                foreach (var erpLine in erpPo.Lines)
                {
                    var item = await GetOrCreateItem(erpLine.ItemCode, erpLine.ItemName);
                    
                    var poLine = new PoLine
                    {
                        PoHeaderId = poHeader.Id,
                        ItemId = item.Id,
                        QtyOrdered = erpLine.QtyOrdered,
                        QtyReceived = 0,
                        UnitPrice = erpLine.UnitPrice
                    };

                    _context.PoLines.Add(poLine);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // Update existing PO if needed
                existingPo.Supplier = erpPo.Supplier;
                
                // Update or add lines
                foreach (var erpLine in erpPo.Lines)
                {
                    var item = await GetOrCreateItem(erpLine.ItemCode, erpLine.ItemName);
                    var existingLine = existingPo.PoLines.FirstOrDefault(pl => pl.ItemId == item.Id);
                    
                    if (existingLine == null)
                    {
                        var poLine = new PoLine
                        {
                            PoHeaderId = existingPo.Id,
                            ItemId = item.Id,
                            QtyOrdered = erpLine.QtyOrdered,
                            QtyReceived = 0,
                            UnitPrice = erpLine.UnitPrice
                        };

                        _context.PoLines.Add(poLine);
                    }
                    else
                    {
                        existingLine.QtyOrdered = erpLine.QtyOrdered;
                        existingLine.UnitPrice = erpLine.UnitPrice;
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task<Item> GetOrCreateItem(string itemCode, string itemName)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Code == itemCode);
            
            if (item == null)
            {
                item = new Item
                {
                    Code = itemCode,
                    Name = itemName ?? itemCode,
                    UoM = "EA",
                    IsConsumable = true,
                    Active = true,
                    CreatedAt = DateTime.Now
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();
            }

            return item;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _context?.Dispose();
        }
    }

    // ERP Integration DTOs
    public class ErpPurchaseOrder
    {
        public string PoNo { get; set; }
        public DateTime PoDate { get; set; }
        public string Supplier { get; set; }
        public List<ErpPurchaseOrderLine> Lines { get; set; } = new List<ErpPurchaseOrderLine>();
    }

    public class ErpPurchaseOrderLine
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal QtyOrdered { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class ReceiptLine
    {
        public string ItemCode { get; set; }
        public decimal QtyReceived { get; set; }
        public string Lot { get; set; }
        public string Serial { get; set; }
    }
}
