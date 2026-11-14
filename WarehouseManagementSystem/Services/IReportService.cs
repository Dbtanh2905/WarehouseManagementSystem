using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public interface IReportService
    {
        Task<List<InventoryItemViewModel>> GetInventoryReportAsync(int? categoryId);
        Task<List<TopIssuedProductViewModel>> GetTopIssuedProductsAsync(DateTime? fromDate, DateTime? toDate, int top = 10);
        Task<byte[]> ExportInventoryToCsvAsync(int? categoryId);
        Task<byte[]> ExportStockTransactionsToCsvAsync(DateTime? fromDate, DateTime? toDate, int? transactionType);
    }
}
