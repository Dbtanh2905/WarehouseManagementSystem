using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;

        public DashboardService(ApplicationDbContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

            // 1. Tổng số loại linh kiện
            int totalProductTypes = await _context.Products.CountAsync(p => p.IsActive);

            // 2. Tổng số lượng linh kiện tồn kho
            int totalInventoryQuantity = await _context.Inventories.SumAsync(i => i.Quantity);

            // 3. Số phiếu nhập trong tháng
            int receiptsThisMonth = await _context.StockReceipts
                .CountAsync(r => r.ReceiptDate >= startOfMonth && r.ReceiptDate <= endOfMonth);

            // 4. Số phiếu xuất trong tháng
            int issuesThisMonth = await _context.StockIssues
                .CountAsync(i => i.IssueDate >= startOfMonth && i.IssueDate <= endOfMonth);

            // 5. Cảnh báo linh kiện dưới mức tồn tối thiểu
            var lowStockItems = await _inventoryService.GetLowStockItemsAsync();
            int lowStockCount = lowStockItems.Count;

            // 6. Dữ liệu biểu đồ nhập/xuất 6 tháng gần nhất
            var chartLabels = new List<string>();
            var stockInQuantities = new List<int>();
            var stockOutQuantities = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                chartLabels.Add($"T{monthDate.Month}/{monthDate.Year}");

                // Tổng số lượng nhập trong tháng
                int monthIn = await _context.StockTransactions
                    .Where(t => t.TransactionType == TransactionType.In && t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd)
                    .SumAsync(t => (int?)t.Quantity) ?? 0;

                // Tổng số lượng xuất trong tháng
                int monthOut = await _context.StockTransactions
                    .Where(t => t.TransactionType == TransactionType.Out && t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd)
                    .SumAsync(t => (int?)t.Quantity) ?? 0;

                stockInQuantities.Add(monthIn);
                stockOutQuantities.Add(monthOut);
            }

            // 7. Giao dịch gần đây (10 giao dịch gần nhất)
            var recentTransactions = await _context.StockTransactions
                .Include(t => t.Product)
                .Include(t => t.Location)
                .Include(t => t.PerformedBy)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            return new DashboardViewModel
            {
                TotalProductTypes = totalProductTypes,
                TotalInventoryQuantity = totalInventoryQuantity,
                StockReceiptsThisMonth = receiptsThisMonth,
                StockIssuesThisMonth = issuesThisMonth,
                LowStockProductCount = lowStockCount,
                ChartLabels = chartLabels,
                MonthlyStockInQuantities = stockInQuantities,
                MonthlyStockOutQuantities = stockOutQuantities,
                RecentTransactions = recentTransactions,
                LowStockItems = lowStockItems.Take(5).ToList()
            };
        }
    }
}
