using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public class StockIssueService : IStockIssueService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockIssueService> _logger;

        public StockIssueService(ApplicationDbContext context, ILogger<StockIssueService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StockIssueListViewModel> GetIssuesAsync(
            string? searchTerm, IssueStatus? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize)
        {
            var query = _context.StockIssues
                .Include(i => i.CreatedBy)
                .Include(i => i.IssueDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(i => i.IssueCode.ToLower().Contains(term) 
                    || (i.Note != null && i.Note.ToLower().Contains(term)));
            }

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(i => i.IssueDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(i => i.IssueDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            int totalItems = await query.CountAsync();
            var issues = await query
                .OrderByDescending(i => i.IssueDate)
                .ThenByDescending(i => i.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new StockIssueListViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                Issues = issues
            };
        }

        public async Task<StockIssue?> GetIssueByIdAsync(int id)
        {
            return await _context.StockIssues
                .Include(i => i.CreatedBy)
                .Include(i => i.IssueDetails)
                    .ThenInclude(d => d.Product)
                .Include(i => i.IssueDetails)
                    .ThenInclude(d => d.Location)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<(bool Success, string Message, int IssueId)> CreateIssueAsync(
            StockIssueCreateViewModel model, string userId)
        {
            if (model.Details == null || model.Details.Count == 0)
            {
                return (false, "Phiếu xuất phải có ít nhất 1 dòng linh kiện.", 0);
            }

            if (model.Details.Any(d => d.Quantity <= 0))
            {
                return (false, "Số lượng xuất của mỗi dòng linh kiện phải lớn hơn 0.", 0);
            }

            bool codeExists = await _context.StockIssues.AnyAsync(i => i.IssueCode.ToLower() == model.IssueCode.Trim().ToLower());
            if (codeExists)
            {
                return (false, $"Mã phiếu xuất '{model.IssueCode}' đã tồn tại trong hệ thống.", 0);
            }

            // Kiểm tra sơ bộ tồn kho khi lập phiếu
            foreach (var item in model.Details)
            {
                var inv = await _context.Inventories
                    .Include(i => i.Product)
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.LocationId == item.LocationId);

                int currentQty = inv?.Quantity ?? 0;
                if (currentQty < item.Quantity)
                {
                    string prodName = inv?.Product?.ProductName ?? $"ID: {item.ProductId}";
                    string locCode = inv?.Location?.LocationCode ?? $"ID: {item.LocationId}";
                    return (false, $"Linh kiện '{prodName}' tại vị trí '{locCode}' chỉ còn tồn {currentQty}, không đủ để xuất {item.Quantity}.", 0);
                }
            }

            var issue = new StockIssue
            {
                IssueCode = model.IssueCode.Trim().ToUpper(),
                IssueDate = model.IssueDate,
                CreatedById = userId,
                Status = IssueStatus.Draft,
                Note = model.Note,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in model.Details)
            {
                issue.IssueDetails.Add(new StockIssueDetail
                {
                    ProductId = item.ProductId,
                    LocationId = item.LocationId,
                    Quantity = item.Quantity
                });
            }

            _context.StockIssues.Add(issue);
            await _context.SaveChangesAsync();

            return (true, "Tạo phiếu xuất kho thành công ở trạng thái bản nháp.", issue.Id);
        }

        public async Task<(bool Success, string Message)> UpdateIssueDraftAsync(
            int id, StockIssueCreateViewModel model, string userId)
        {
            var issue = await _context.StockIssues
                .Include(i => i.IssueDetails)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
            {
                return (false, "Không tìm thấy phiếu xuất kho.");
            }

            if (issue.Status != IssueStatus.Draft)
            {
                return (false, "Chỉ có thể chỉnh sửa phiếu xuất ở trạng thái bản nháp.");
            }

            if (model.Details == null || model.Details.Count == 0)
            {
                return (false, "Phiếu xuất phải có ít nhất 1 dòng linh kiện.");
            }

            if (model.Details.Any(d => d.Quantity <= 0))
            {
                return (false, "Số lượng xuất phải lớn hơn 0.");
            }

            if (!string.Equals(issue.IssueCode, model.IssueCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                bool codeExists = await _context.StockIssues.AnyAsync(i => i.Id != id && i.IssueCode.ToLower() == model.IssueCode.Trim().ToLower());
                if (codeExists)
                {
                    return (false, $"Mã phiếu xuất '{model.IssueCode}' đã được sử dụng.");
                }
                issue.IssueCode = model.IssueCode.Trim().ToUpper();
            }

            issue.IssueDate = model.IssueDate;
            issue.Note = model.Note;

            _context.StockIssueDetails.RemoveRange(issue.IssueDetails);

            foreach (var item in model.Details)
            {
                issue.IssueDetails.Add(new StockIssueDetail
                {
                    StockIssueId = issue.Id,
                    ProductId = item.ProductId,
                    LocationId = item.LocationId,
                    Quantity = item.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return (true, "Cập nhật phiếu xuất kho bản nháp thành công.");
        }

        public async Task<(bool Success, string Message)> CancelIssueAsync(int id, string userId)
        {
            var issue = await _context.StockIssues.FindAsync(id);
            if (issue == null) return (false, "Không tìm thấy phiếu xuất kho.");

            if (issue.Status != IssueStatus.Draft)
            {
                return (false, "Chỉ có thể hủy phiếu xuất đang ở trạng thái bản nháp.");
            }

            issue.Status = IssueStatus.Cancelled;
            await _context.SaveChangesAsync();
            return (true, "Đã hủy phiếu xuất kho thành công.");
        }

        public async Task<(bool Success, string Message)> ConfirmIssueAsync(int id, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var issue = await _context.StockIssues
                    .Include(i => i.IssueDetails)
                        .ThenInclude(d => d.Product)
                    .Include(i => i.IssueDetails)
                        .ThenInclude(d => d.Location)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (issue == null)
                {
                    return (false, "Không tìm thấy phiếu xuất kho.");
                }

                if (issue.Status == IssueStatus.Confirmed)
                {
                    return (false, "Phiếu này đã được xuất kho trước đó. Không thể xác nhận lần thứ hai.");
                }

                if (issue.Status == IssueStatus.Cancelled)
                {
                    return (false, "Phiếu xuất kho đã bị hủy. Không thể xác nhận.");
                }

                if (issue.IssueDetails.Count == 0)
                {
                    return (false, "Phiếu xuất kho không có chi tiết linh kiện nào.");
                }

                // Nhóm số lượng xuất theo từng cặp (ProductId, LocationId) trong trường hợp 1 phiếu có 2 dòng cùng vị trí
                var aggregatedRequests = issue.IssueDetails
                    .GroupBy(d => new { d.ProductId, d.LocationId })
                    .Select(g => new
                    {
                        g.Key.ProductId,
                        g.Key.LocationId,
                        TotalRequired = g.Sum(x => x.Quantity),
                        ProductName = g.First().Product?.ProductName ?? $"SP #{g.Key.ProductId}",
                        LocationCode = g.First().Location?.LocationCode ?? $"VT #{g.Key.LocationId}"
                    }).ToList();

                // Kiểm tra tồn kho tại từng vị trí thực tế
                foreach (var req in aggregatedRequests)
                {
                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(inv => inv.ProductId == req.ProductId && inv.LocationId == req.LocationId);

                    int available = inventory?.Quantity ?? 0;
                    if (available < req.TotalRequired)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Không thể xuất kho: Linh kiện '{req.ProductName}' tại vị trí '{req.LocationCode}' hiện chỉ còn tồn {available}, không đủ để xuất {req.TotalRequired}. Giao dịch đã được hủy bỏ và không làm thay đổi tồn kho.");
                    }

                    // Giảm tồn kho
                    inventory!.Quantity -= req.TotalRequired;
                    inventory.UpdatedAt = DateTime.UtcNow;

                    // Kiểm tra an toàn tuyệt đối không âm
                    if (inventory.Quantity < 0)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Lỗi vi phạm toàn vẹn dữ liệu: Tồn kho của '{req.ProductName}' không được phép âm.");
                    }
                }

                // Tạo StockTransactions cho từng chi tiết
                foreach (var detail in issue.IssueDetails)
                {
                    var stockTx = new StockTransaction
                    {
                        ProductId = detail.ProductId,
                        LocationId = detail.LocationId,
                        TransactionType = TransactionType.Out,
                        Quantity = detail.Quantity,
                        ReferenceCode = issue.IssueCode,
                        PerformedById = userId,
                        CreatedAt = DateTime.UtcNow,
                        Note = $"Xuất kho theo phiếu {issue.IssueCode}: {issue.Note}"
                    };
                    _context.StockTransactions.Add(stockTx);
                }

                // Cập nhật trạng thái phiếu
                issue.Status = IssueStatus.Confirmed;
                issue.ConfirmedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Xác nhận phiếu xuất {IssueCode} thành công bởi user {UserId}", issue.IssueCode, userId);
                return (true, $"Xác nhận xuất kho phiếu {issue.IssueCode} thành công. Tồn kho đã được cập nhật.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Xung đột đồng thời (Concurrency Exception) khi xuất phiếu {Id}", id);
                return (false, "Dữ liệu tồn kho vừa được cập nhật bởi một người dùng khác. Vui lòng kiểm tra lại số lượng tồn khả dụng và thử lại.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xảy ra khi xác nhận xuất phiếu {Id}", id);
                return (false, "Lỗi hệ thống khi xác nhận xuất kho: " + ex.Message);
            }
        }

        public async Task<string> GenerateIssueCodeAsync()
        {
            string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            string prefix = $"PX-{datePart}-";

            var lastIssue = await _context.StockIssues
                .Where(i => i.IssueCode.StartsWith(prefix))
                .OrderByDescending(i => i.IssueCode)
                .Select(i => i.IssueCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastIssue))
            {
                var parts = lastIssue.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int num))
                {
                    nextNumber = num + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}
