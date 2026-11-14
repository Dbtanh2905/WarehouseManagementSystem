using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public interface IInventoryService
    {
        Task<InventoryFilterViewModel> GetInventoryListAsync(string? searchTerm, int? categoryId, StockStatus? status, int pageNumber, int pageSize);
        Task<InventoryItemViewModel?> GetProductInventoryDetailAsync(int productId);
        Task<int> GetStockAtLocationAsync(int productId, int locationId);
        Task<int> GetTotalStockAsync(int productId);
        Task<List<InventoryItemViewModel>> GetLowStockItemsAsync();
        Task<List<StockTransaction>> GetProductTransactionHistoryAsync(int productId);
    }
}
