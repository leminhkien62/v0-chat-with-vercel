using Microsoft.AspNet.SignalR;
using QRCoder;
using Rotativa;
using System;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WmsSystem.Data;
using WmsSystem.Hubs;
using WmsSystem.Models;
using WmsSystem.ViewModels;

namespace WmsSystem.Services
{
    public class ReceivingService
    {
        private readonly WmsDbContext _context;
        private readonly IHubContext _hubContext;

        public ReceivingService(WmsDbContext context)
        {
            _context = context;
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<WmsHub>();
        }

        public async Task<Lpn> ReceivePurchaseOrderAsync(ReceivePoViewModel viewModel)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Validate location
                    var location = await _context.Locations
                        .Include(l => l.Warehouse)
                        .FirstOrDefaultAsync(l => l.Id == viewModel.SelectedLocationId);

                    if (location == null)
                        throw new InvalidOperationException("Selected location not found.");

                    if (location.Locked)
                        throw new InvalidOperationException("Cannot receive to locked location.");

                    // Generate LPN
                    var lpnCode = GenerateLpnCode();
                    var lpn = new Lpn
                    {
                        LpnCode = lpnCode,
                        LocationId = viewModel.SelectedLocationId,
                        Note = $"Received from PO: {viewModel.Po.PoNo}",
                        CreatedAt = DateTime.Now
                    };

                    _context.Lpns.Add(lpn);
                    await _context.SaveChangesAsync();

                    // Process each receive line
                    foreach (var receiveLine in viewModel.ReceiveLines.Where(rl => rl.QtyToReceive > 0))
                    {
                        var poLine = await _context.PoLines
                            .Include(pl => pl.Item)
                            .FirstOrDefaultAsync(pl => pl.Id == receiveLine.PoLineId);

                        if (poLine == null) continue;

                        // Validate quantity
                        var remainingQty = poLine.QtyOrdered - poLine.QtyReceived;
                        if (receiveLine.QtyToReceive > remainingQty)
                            throw new InvalidOperationException($"Cannot receive more than remaining quantity for item {poLine.Item.Code}");

                        // Update PO line
                        poLine.QtyReceived += receiveLine.QtyToReceive;

                        // Create LPN item
                        var lpnItem = new LpnItem
                        {
                            LpnId = lpn.Id,
                            ItemId = poLine.ItemId,
                            Qty = receiveLine.QtyToReceive,
                            Lot = receiveLine.Lot,
                            Serial = receiveLine.Serial,
                            ExpiryDate = receiveLine.ExpiryDate
                        };

                        _context.LpnItems.Add(lpnItem);

                        // Create or update stock
                        var stock = await _context.Stocks
                            .FirstOrDefaultAsync(s => s.ItemId == poLine.ItemId && 
                                                    s.LocationId == viewModel.SelectedLocationId &&
                                                    s.Lot == receiveLine.Lot &&
                                                    s.Serial == receiveLine.Serial);

                        if (stock == null)
                        {
                            stock = new Stock
                            {
                                ItemId = poLine.ItemId,
                                LocationId = viewModel.SelectedLocationId,
                                QtyOnHand = receiveLine.QtyToReceive,
                                QtyAllocated = 0,
                                Lot = receiveLine.Lot,
                                Serial = receiveLine.Serial,
                                Lpn = lpnCode,
                                ExpiryDate = receiveLine.ExpiryDate,
                                ReceivedDate = DateTime.Now
                            };
                            _context.Stocks.Add(stock);
                        }
                        else
                        {
                            stock.QtyOnHand += receiveLine.QtyToReceive;
                        }

                        // Create transaction record
                        var txn = new Transaction
                        {
                            Type = TransactionType.Receive,
                            ItemId = poLine.ItemId,
                            ToLocationId = viewModel.SelectedLocationId,
                            Qty = receiveLine.QtyToReceive,
                            RefNo = viewModel.Po.PoNo,
                            UserName = HttpContext.Current.User.Identity.Name,
                            Note = $"Received to LPN: {lpnCode}"
                        };

                        _context.Transactions.Add(txn);
                    }

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    // Broadcast real-time update
                    await BroadcastReceivingUpdate(lpn);

                    return lpn;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task PutawayLpnAsync(int lpnId, int targetLocationId, string userName)
        {
            var lpn = await _context.Lpns
                .Include(l => l.LpnItems.Select(li => li.Item))
                .FirstOrDefaultAsync(l => l.Id == lpnId);

            if (lpn == null)
                throw new InvalidOperationException("LPN not found.");

            if (lpn.Closed)
                throw new InvalidOperationException("LPN is already closed.");

            var targetLocation = await _context.Locations.FindAsync(targetLocationId);
            if (targetLocation == null)
                throw new InvalidOperationException("Target location not found.");

            if (targetLocation.Locked)
                throw new InvalidOperationException("Cannot putaway to locked location.");

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Move stock to target location
                    foreach (var lpnItem in lpn.LpnItems)
                    {
                        // Find current stock
                        var currentStock = await _context.Stocks
                            .FirstOrDefaultAsync(s => s.ItemId == lpnItem.ItemId &&
                                                    s.LocationId == lpn.LocationId &&
                                                    s.Lpn == lpn.LpnCode);

                        if (currentStock != null && currentStock.QtyOnHand >= lpnItem.Qty)
                        {
                            // Reduce current stock
                            currentStock.QtyOnHand -= lpnItem.Qty;
                            if (currentStock.QtyOnHand == 0)
                                _context.Stocks.Remove(currentStock);

                            // Create or update target stock
                            var targetStock = await _context.Stocks
                                .FirstOrDefaultAsync(s => s.ItemId == lpnItem.ItemId &&
                                                        s.LocationId == targetLocationId &&
                                                        s.Lot == lpnItem.Lot &&
                                                        s.Serial == lpnItem.Serial);

                            if (targetStock == null)
                            {
                                targetStock = new Stock
                                {
                                    ItemId = lpnItem.ItemId,
                                    LocationId = targetLocationId,
                                    QtyOnHand = lpnItem.Qty,
                                    QtyAllocated = 0,
                                    Lot = lpnItem.Lot,
                                    Serial = lpnItem.Serial,
                                    Lpn = lpn.LpnCode,
                                    ExpiryDate = lpnItem.ExpiryDate,
                                    ReceivedDate = currentStock?.ReceivedDate ?? DateTime.Now
                                };
                                _context.Stocks.Add(targetStock);
                            }
                            else
                            {
                                targetStock.QtyOnHand += lpnItem.Qty;
                            }

                            // Create move transaction
                            var txn = new Transaction
                            {
                                Type = TransactionType.Move,
                                ItemId = lpnItem.ItemId,
                                FromLocationId = lpn.LocationId,
                                ToLocationId = targetLocationId,
                                Qty = lpnItem.Qty,
                                RefNo = lpn.LpnCode,
                                UserName = userName,
                                Note = "Putaway operation"
                            };

                            _context.Transactions.Add(txn);
                        }
                    }

                    // Update LPN location and close it
                    lpn.LocationId = targetLocationId;
                    lpn.Closed = true;

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    // Broadcast real-time update
                    await BroadcastPutawayUpdate(lpn);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public string GenerateQrCodeData(Lpn lpn)
        {
            return $"LPN:{lpn.LpnCode}|LOC:{lpn.Location?.Code}|DATE:{lpn.CreatedAt:yyyy-MM-dd}";
        }

        public async Task<byte[]> GenerateLpnPdfAsync(int lpnId, string paperSize)
        {
            var lpn = await _context.Lpns
                .Include(l => l.Location.Warehouse)
                .Include(l => l.LpnItems.Select(li => li.Item))
                .FirstOrDefaultAsync(l => l.Id == lpnId);

            if (lpn == null)
                throw new InvalidOperationException("LPN not found.");

            var viewModel = new PrintLpnViewModel
            {
                Lpn = lpn,
                QrCodeData = GenerateQrCodeData(lpn),
                QrCodeImage = GenerateQrCodeImage(GenerateQrCodeData(lpn))
            };

            var actionResult = new ViewAsPdf("LpnPrintTemplate", viewModel)
            {
                PageSize = paperSize == "A5" ? Rotativa.Options.Size.A5 : Rotativa.Options.Size.A6,
                PageOrientation = Rotativa.Options.Orientation.Portrait,
                PageMargins = new Rotativa.Options.Margins(5, 5, 5, 5)
            };

            return actionResult.BuildFile(new ControllerContext());
        }

        private string GenerateQrCodeImage(string data)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var qrCodeImage = qrCode.GetGraphic(20))
                    {
                        using (var stream = new MemoryStream())
                        {
                            qrCodeImage.Save(stream, ImageFormat.Png);
                            var imageBytes = stream.ToArray();
                            return Convert.ToBase64String(imageBytes);
                        }
                    }
                }
            }
        }

        private string GenerateLpnCode()
        {
            return $"LPN-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks.ToString().Substring(10)}";
        }

        private async Task BroadcastReceivingUpdate(Lpn lpn)
        {
            var updateData = new
            {
                Type = "Receiving",
                LpnCode = lpn.LpnCode,
                LocationCode = lpn.Location?.Code,
                Timestamp = DateTime.Now
            };

            _hubContext.Clients.All.receivingUpdate(updateData);
        }

        private async Task BroadcastPutawayUpdate(Lpn lpn)
        {
            var updateData = new
            {
                Type = "Putaway",
                LpnCode = lpn.LpnCode,
                LocationCode = lpn.Location?.Code,
                Timestamp = DateTime.Now
            };

            _hubContext.Clients.All.putawayUpdate(updateData);
        }
    }
}
