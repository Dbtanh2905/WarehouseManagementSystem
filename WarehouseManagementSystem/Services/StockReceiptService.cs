using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public class StockReceiptService : IStockReceiptService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockReceiptService> _logger;

        public StockReceiptService(ApplicationDbContext context, ILogger<StockReceiptService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StockReceiptListViewModel> GetReceiptsAsync(
            string? searchTerm, ReceiptStatus? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize)
        {
            var query = _context.StockReceipts
                .Include(r => r.Supplier)
                .Include(r => r.CreatedBy)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(r => r.ReceiptCode.ToLower().Contains(term) 
                    || (r.Supplier != null && r.Supplier.SupplierName.ToLower().Contains(term))
                    || (r.Note != null && r.Note.ToLower().Contains(term)));
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.ReceiptDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.ReceiptDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            int totalItems = await query.CountAsync();
            var receipts = await query
                .OrderByDescending(r => r.ReceiptDate)
                .ThenByDescending(r => r.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new StockReceiptListViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                Receipts = receipts
            };
        }

        public async Task<StockReceipt?> GetReceiptByIdAsync(int id)
        {
            return await _context.StockReceipts
                .Include(r => r.Supplier)
                .Include(r => r.CreatedBy)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.Product)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.Location)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<(bool Success, string Message, int ReceiptId)> CreateReceiptAsync(
            StockReceiptCreateViewModel model, string userId)
        {
            if (model.Details == null || model.Details.Count == 0)
            {
                return (false, "Phiếu nhập phải có ít nhất 1 dòng linh kiện.", 0);
            }

            // Kiểm tra số lượng > 0
            if (model.Details.Any(d => d.Quantity <= 0))
            {
                return (false, "Số lượng nhập của mỗi dòng linh kiện phải lớn hơn 0.", 0);
            }

            // Kiểm tra trùng mã phiếu
            bool codeExists = await _context.StockReceipts.AnyAsync(r => r.ReceiptCode.ToLower() == model.ReceiptCode.Trim().ToLower());
            if (codeExists)
            {
                return (false, $"Mã phiếu nhập '{model.ReceiptCode}' đã tồn tại trong hệ thống.", 0);
            }

            var receipt = new StockReceipt
            {
                ReceiptCode = model.ReceiptCode.Trim().ToUpper(),
                ReceiptDate = model.ReceiptDate,
                SupplierId = model.SupplierId,
                CreatedById = userId,
                Status = ReceiptStatus.Draft,
                Note = model.Note,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in model.Details)
            {
                receipt.ReceiptDetails.Add(new StockReceiptDetail
                {
                    ProductId = item.ProductId,
                    LocationId = item.LocationId,
                    Quantity = item.Quantity
                });
            }

            _context.StockReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            return (true, "Tạo phiếu nhập thành công ở trạng thái bản nháp.", receipt.Id);
        }

        public async Task<(bool Success, string Message)> UpdateReceiptDraftAsync(
            int id, StockReceiptCreateViewModel model, string userId)
        {
            var receipt = await _context.StockReceipts
                .Include(r => r.ReceiptDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                return (false, "Không tìm thấy phiếu nhập.");
            }

            if (receipt.Status != ReceiptStatus.Draft)
            {
                return (false, "Chỉ có thể chỉnh sửa phiếu nhập ở trạng thái bản nháp.");
            }

            if (model.Details == null || model.Details.Count == 0)
            {
                return (false, "Phiếu nhập phải có ít nhất 1 dòng linh kiện.");
            }

            if (model.Details.Any(d => d.Quantity <= 0))
            {
                return (false, "Số lượng nhập phải lớn hơn 0.");
            }

            // Kiểm tra mã phiếu nếu thay đổi
            if (!string.Equals(receipt.ReceiptCode, model.ReceiptCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                bool codeExists = await _context.StockReceipts.AnyAsync(r => r.Id != id && r.ReceiptCode.ToLower() == model.ReceiptCode.Trim().ToLower());
                if (codeExists)
                {
                    return (false, $"Mã phiếu '{model.ReceiptCode}' đã được sử dụng bởi phiếu khác.");
                }
                receipt.ReceiptCode = model.ReceiptCode.Trim().ToUpper();
            }

            receipt.ReceiptDate = model.ReceiptDate;
            receipt.SupplierId = model.SupplierId;
            receipt.Note = model.Note;

            // Cập nhật lại chi tiết: xóa cũ thêm mới
            _context.StockReceiptDetails.RemoveRange(receipt.ReceiptDetails);

            foreach (var item in model.Details)
            {
                receipt.ReceiptDetails.Add(new StockReceiptDetail
                {
                    StockReceiptId = receipt.Id,
                    ProductId = item.ProductId,
                    LocationId = item.LocationId,
                    Quantity = item.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return (true, "Cập nhật phiếu nhập bản nháp thành công.");
        }

        public async Task<(bool Success, string Message)> CancelReceiptAsync(int id, string userId)
        {
            var receipt = await _context.StockReceipts.FindAsync(id);
            if (receipt == null) return (false, "Không tìm thấy phiếu nhập.");

            if (receipt.Status != ReceiptStatus.Draft)
            {
                return (false, "Chỉ có thể hủy phiếu nhập đang ở trạng thái bản nháp.");
            }

            receipt.Status = ReceiptStatus.Cancelled;
            await _context.SaveChangesAsync();
            return (true, "Đã hủy phiếu nhập kho thành công.");
        }

        public async Task<(bool Success, string Message)> ConfirmReceiptAsync(int id, string userId)
        {
            // Sử dụng Database Transaction để đảm bảo tính toàn vẹn
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var receipt = await _context.StockReceipts
                    .Include(r => r.ReceiptDetails)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (receipt == null)
                {
                    return (false, "Không tìm thấy phiếu nhập kho.");
                }

                if (receipt.Status == ReceiptStatus.Confirmed)
                {
                    return (false, "Phiếu này đã được xác nhận trước đó. Không thể xác nhận lần thứ hai.");
                }

                if (receipt.Status == ReceiptStatus.Cancelled)
                {
                    return (false, "Phiếu nhập đã bị hủy. Không thể xác nhận.");
                }

                if (receipt.ReceiptDetails.Count == 0)
                {
                    return (false, "Phiếu nhập không có chi tiết linh kiện nào.");
                }

                // Cập nhật trạng thái phiếu
                receipt.Status = ReceiptStatus.Confirmed;
                receipt.ConfirmedAt = DateTime.UtcNow;

                // Tăng tồn kho và tạo StockTransactions
                foreach (var detail in receipt.ReceiptDetails)
                {
                    if (detail.Quantity <= 0)
                    {
                        await transaction.RollbackAsync();
                        return (false, "Phát hiện số lượng nhập không hợp lệ (nhỏ hơn hoặc bằng 0).");
                    }

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == detail.ProductId && i.LocationId == detail.LocationId);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            ProductId = detail.ProductId,
                            LocationId = detail.LocationId,
                            Quantity = detail.Quantity,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Inventories.Add(inventory);
                    }
                    else
                    {
                        inventory.Quantity += detail.Quantity;
                        inventory.UpdatedAt = DateTime.UtcNow;
                    }

                    // Ghi sổ cái biến động kho (Audit Trail)
                    var stockTx = new StockTransaction
                    {
                        ProductId = detail.ProductId,
                        LocationId = detail.LocationId,
                        TransactionType = TransactionType.In,
                        Quantity = detail.Quantity,
                        ReferenceCode = receipt.ReceiptCode,
                        PerformedById = userId,
                        CreatedAt = DateTime.UtcNow,
                        Note = $"Nhập kho theo phiếu {receipt.ReceiptCode}"
                    };
                    _context.StockTransactions.Add(stockTx);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Xác nhận phiếu nhập {ReceiptCode} thành công bởi user {UserId}", receipt.ReceiptCode, userId);
                return (true, $"Xác nhận phiếu nhập {receipt.ReceiptCode} thành công. Tồn kho đã được cập nhật.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xảy ra khi xác nhận phiếu nhập {Id}", id);
                return (false, "Lỗi hệ thống khi xác nhận nhập kho: " + ex.Message);
            }
        }

        public async Task<string> GenerateReceiptCodeAsync()
        {
            string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            string prefix = $"PN-{datePart}-";

            var lastReceipt = await _context.StockReceipts
                .Where(r => r.ReceiptCode.StartsWith(prefix))
                .OrderByDescending(r => r.ReceiptCode)
                .Select(r => r.ReceiptCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastReceipt))
            {
                var parts = lastReceipt.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int num))
                {
                    nextNumber = num + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}
