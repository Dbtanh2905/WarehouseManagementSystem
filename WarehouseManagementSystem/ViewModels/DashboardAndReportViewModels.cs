using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProductTypes { get; set; }
        public int TotalInventoryQuantity { get; set; }
        public int StockReceiptsThisMonth { get; set; }
        public int StockIssuesThisMonth { get; set; }
        public int LowStockProductCount { get; set; }

        // Dữ liệu biểu đồ nhập/xuất 6 tháng gần nhất
        public List<string> ChartLabels { get; set; } = new();
        public List<int> MonthlyStockInQuantities { get; set; } = new();
        public List<int> MonthlyStockOutQuantities { get; set; } = new();

        // Giao dịch gần đây
        public List<StockTransaction> RecentTransactions { get; set; } = new();

        // Cảnh báo linh kiện dưới mức tồn tối thiểu
        public List<InventoryItemViewModel> LowStockItems { get; set; } = new();
    }

    public class ReportFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? CategoryId { get; set; }
        public string ReportType { get; set; } = "Inventory"; // Inventory, StockIn, StockOut, LowStock, TopIssued
    }

    public class TopIssuedProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TotalQuantityIssued { get; set; }
        public int CurrentStock { get; set; }
    }
}
