using Microsoft.AspNet.SignalR;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WmsSystem.Data;
using WmsSystem.Hubs;
using WmsSystem.Models;
using WmsSystem.ViewModels;

namespace WmsSystem.Services
{
    public class IssueService
    {
        private readonly WmsDbContext _context;
        private readonly IHubContext _hubContext;

        public IssueService(WmsDbContext context)
        {
            _context = context;
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<WmsHub>();
        }

        public async Task<int> CreateManualIssueAsync(CreateIssueViewModel viewModel, string userName)
        {
            // Create a temporary request for manual issue
            var request = new Request
            {
                CreatedAt = DateTime.Now,
                DeptId = 1, // Default department for manual issues
                Requester = userName,
                ItemId = viewModel.ItemId,
                Qty = viewModel.Qty,
                Status = RequestStatus.Processing,
                Note = viewModel.Note ?? "Manual issue"
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            return request.Id;
        }

        public async Task ProcessRequestAsync(ProcessRequestViewModel viewModel, string userName)
        {
            var request = await _context.Requests.FindAsync(viewModel.Request.Id);
            if (request == null)
                throw new InvalidOperationException("Request not found.");

            if (request.Status != RequestStatus.New)
                throw new InvalidOperationException("Request has already been processed.");

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    decimal totalIssued = 0;
                    
                    foreach (var pickItem in viewModel.PickList.Where(p => p.QtyToPick > 0))
                    {
                        var stock = await _context.Stocks
                            .FirstOrDefaultAsync(s => s.Id == pickItem.StockId);

                        if (stock == null) continue;

                        var availableQty = stock.QtyOnHand - stock.QtyAllocated;
                        var qtyToIssue = Math.Min(pickItem.QtyToPick, availableQty);

                        if (qtyToIssue <= 0) continue;

                        // Update stock
                        stock.QtyOnHand -= qtyToIssue;
                        totalIssued += qtyToIssue;

                        // Create transaction record
                        var txn = new Transaction
                        {
                            Type = TransactionType.Issue,
                            ItemId = request.ItemId,
                            FromLocationId = stock.LocationId,
                            Qty = qtyToIssue,
                            RefNo = $"REQ-{request.Id}",
                            UserName = userName,
                            Note = $"Issue for {request.Department?.Name ?? "Manual"} - {request.Requester}"
                        };

                        _context.Transactions.Add(txn);

                        // Remove stock record if quantity becomes zero
                        if (stock.QtyOnHand <= 0)
                            _context.Stocks.Remove(stock);
                    }

                    // Update request status
                    if (totalIssued >= request.Qty)
                    {
                        request.Status = RequestStatus.Completed;
                    }
                    else if (totalIssued > 0)
                    {
                        request.Status = RequestStatus.Processing;
                        request.Note += $" (Partial: {totalIssued}/{request.Qty})";
                    }
                    else
                    {
                        throw new InvalidOperationException("No items were issued. Check stock availability.");
                    }

                    request.ProcessedBy = userName;
                    request.ProcessedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    // Broadcast real-time update
                    await BroadcastIssueUpdate(request, totalIssued);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task CreateMoveAsync(CreateMoveViewModel viewModel, string userName)
        {
            var fromStock = await _context.Stocks
                .Include(s => s.Item)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == viewModel.StockId);

            if (fromStock == null)
                throw new InvalidOperationException("Source stock not found.");

            var toLocation = await _context.Locations.FindAsync(viewModel.ToLocationId);
            if (toLocation == null)
                throw new InvalidOperationException("Target location not found.");

            if (toLocation.Locked)
                throw new InvalidOperationException("Cannot move to locked location.");

            var availableQty = fromStock.QtyOnHand - fromStock.QtyAllocated;
            if (viewModel.Qty > availableQty)
                throw new InvalidOperationException($"Insufficient stock. Available: {availableQty}");

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Update source stock
                    fromStock.QtyOnHand -= viewModel.Qty;

                    // Create or update target stock
                    var toStock = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.ItemId == fromStock.ItemId &&
                                                s.LocationId == viewModel.ToLocationId &&
                                                s.Lot == fromStock.Lot &&
                                                s.Serial == fromStock.Serial);

                    if (toStock == null)
                    {
                        toStock = new Stock
                        {
                            ItemId = fromStock.ItemId,
                            LocationId = viewModel.ToLocationId,
                            QtyOnHand = viewModel.Qty,
                            QtyAllocated = 0,
                            Lot = fromStock.Lot,
                            Serial = fromStock.Serial,
                            Lpn = fromStock.Lpn,
                            ExpiryDate = fromStock.ExpiryDate,
                            ReceivedDate = fromStock.ReceivedDate
                        };
                        _context.Stocks.Add(toStock);
                    }
                    else
                    {
                        toStock.QtyOnHand += viewModel.Qty;
                    }

                    // Remove source stock if quantity becomes zero
                    if (fromStock.QtyOnHand <= 0)
                        _context.Stocks.Remove(fromStock);

                    // Create transaction record
                    var txn = new Transaction
                    {
                        Type = TransactionType.Move,
                        ItemId = fromStock.ItemId,
                        FromLocationId = fromStock.LocationId,
                        ToLocationId = viewModel.ToLocationId,
                        Qty = viewModel.Qty,
                        RefNo = viewModel.RefNo,
                        UserName = userName,
                        Note = viewModel.Note
                    };

                    _context.Transactions.Add(txn);

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    // Broadcast real-time update
                    await BroadcastMoveUpdate(txn);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private async Task BroadcastIssueUpdate(Request request, decimal qtyIssued)
        {
            var updateData = new
            {
                Type = "Issue",
                RequestId = request.Id,
                ItemCode = request.Item?.Code,
                QtyIssued = qtyIssued,
                Requester = request.Requester,
                Timestamp = DateTime.Now
            };

            _hubContext.Clients.All.issueUpdate(updateData);
        }

        private async Task BroadcastMoveUpdate(Transaction transaction)
        {
            var updateData = new
            {
                Type = "Move",
                ItemCode = transaction.Item?.Code,
                FromLocation = transaction.FromLocation?.Code,
                ToLocation = transaction.ToLocation?.Code,
                Qty = transaction.Qty,
                Timestamp = DateTime.Now
            };

            _hubContext.Clients.All.moveUpdate(updateData);
        }
    }
}
