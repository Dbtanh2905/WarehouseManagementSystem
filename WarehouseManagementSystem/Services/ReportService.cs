using Microsoft.EntityFrameworkCore;
using System.Text;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryItemViewModel>> GetInventoryReportAsync(int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                    .ThenInclude(i => i.Location)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query.ToListAsync();

            return products.Select(p =>
            {
                int totalQty = p.Inventories.Sum(i => i.Quantity);
                StockStatus status = totalQty == 0 ? StockStatus.OutOfStock
                    : (totalQty <= p.MinimumStock ? StockStatus.LowStock : StockStatus.Normal);

                return new InventoryItemViewModel
                {
                    ProductId = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    CategoryName = p.Category?.CategoryName ?? "",
                    Unit = p.Unit,
                    TotalQuantity = totalQty,
                    MinimumStock = p.MinimumStock,
                    Status = status,
                    Locations = p.Inventories.Select(i => new LocationStockViewModel
                    {
                        LocationId = i.LocationId,
                        LocationCode = i.Location?.LocationCode ?? "",
                        LocationName = i.Location?.LocationName ?? "",
                        Quantity = i.Quantity,
                        UpdatedAt = i.UpdatedAt
                    }).ToList()
                };
            }).OrderBy(x => x.TotalQuantity).ToList();
        }

        public async Task<List<TopIssuedProductViewModel>> GetTopIssuedProductsAsync(DateTime? fromDate, DateTime? toDate, int top = 10)
        {
            var query = _context.StockTransactions
                .Where(t => t.TransactionType == TransactionType.Out)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            var grouped = await query
                .GroupBy(t => t.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantityIssued = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantityIssued)
                .Take(top)
                .ToListAsync();

            var result = new List<TopIssuedProductViewModel>();
            foreach (var item in grouped)
            {
                var prod = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventories)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (prod != null)
                {
                    result.Add(new TopIssuedProductViewModel
                    {
                        ProductId = prod.Id,
                        ProductCode = prod.ProductCode,
                        ProductName = prod.ProductName,
                        CategoryName = prod.Category?.CategoryName ?? "",
                        Unit = prod.Unit,
                        TotalQuantityIssued = item.TotalQuantityIssued,
                        CurrentStock = prod.Inventories.Sum(i => i.Quantity)
                    });
                }
            }

            return result;
        }

        public async Task<byte[]> ExportInventoryToCsvAsync(int? categoryId)
        {
            var data = await GetInventoryReportAsync(categoryId);

            var sb = new StringBuilder();
            sb.AppendLine("Mã linh kiện,Tên linh kiện,Danh mục,Đơn vị,Tổng tồn kho,Mức tồn tối thiểu,Trạng thái");

            foreach (var item in data)
            {
                string statusText = item.Status switch
                {
                    StockStatus.OutOfStock => "Hết hàng",
                    StockStatus.LowStock => "Sắp hết hàng",
                    _ => "Bình thường"
                };

                // Escape dấu phẩy trong tên
                string escapedName = $"\"{item.ProductName.Replace("\"", "\"\"")}\"";
                string escapedCat = $"\"{item.CategoryName.Replace("\"", "\"\"")}\"";

                sb.AppendLine($"{item.ProductCode},{escapedName},{escapedCat},{item.Unit},{item.TotalQuantity},{item.MinimumStock},{statusText}");
            }

            // Thêm UTF-8 BOM
            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var resultBytes = new byte[preamble.Length + contentBytes.Length];

            Buffer.BlockCopy(preamble, 0, resultBytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, resultBytes, preamble.Length, contentBytes.Length);

            return resultBytes;
        }

        public async Task<byte[]> ExportStockTransactionsToCsvAsync(DateTime? fromDate, DateTime? toDate, int? transactionType)
        {
            var query = _context.StockTransactions
                .Include(t => t.Product)
                .Include(t => t.Location)
                .Include(t => t.PerformedBy)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (transactionType.HasValue && transactionType.Value > 0)
            {
                query = query.Where(t => (int)t.TransactionType == transactionType.Value);
            }

            var transactions = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Thời gian,Mã chứng từ,Loại giao dịch,Mã linh kiện,Tên linh kiện,Vị trí kho,Số lượng,Người thực hiện,Ghi chú");

            foreach (var t in transactions)
            {
                string typeText = t.TransactionType == TransactionType.In ? "Nhập kho" : "Xuất kho";
                string prodName = $"\"{(t.Product?.ProductName ?? "").Replace("\"", "\"\"")}\"";
                string note = $"\"{(t.Note ?? "").Replace("\"", "\"\"")}\"";
                string user = $"\"{(t.PerformedBy?.FullName ?? t.PerformedBy?.UserName ?? "").Replace("\"", "\"\"")}\"";

                sb.AppendLine($"{t.CreatedAt:yyyy-MM-dd HH:mm:ss},{t.ReferenceCode},{typeText},{t.Product?.ProductCode},{prodName},{t.Location?.LocationCode},{t.Quantity},{user},{note}");
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var resultBytes = new byte[preamble.Length + contentBytes.Length];

            Buffer.BlockCopy(preamble, 0, resultBytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, resultBytes, preamble.Length, contentBytes.Length);

            return resultBytes;
        }
    }
}
