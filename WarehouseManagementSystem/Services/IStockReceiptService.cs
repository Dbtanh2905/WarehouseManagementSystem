using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public interface IStockReceiptService
    {
        Task<StockReceiptListViewModel> GetReceiptsAsync(string? searchTerm, ReceiptStatus? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize);
        Task<StockReceipt?> GetReceiptByIdAsync(int id);
        Task<(bool Success, string Message, int ReceiptId)> CreateReceiptAsync(StockReceiptCreateViewModel model, string userId);
        Task<(bool Success, string Message)> UpdateReceiptDraftAsync(int id, StockReceiptCreateViewModel model, string userId);
        Task<(bool Success, string Message)> CancelReceiptAsync(int id, string userId);
        Task<(bool Success, string Message)> ConfirmReceiptAsync(int id, string userId);
        Task<string> GenerateReceiptCodeAsync();
    }
}
