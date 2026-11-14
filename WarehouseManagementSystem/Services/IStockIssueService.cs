using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public interface IStockIssueService
    {
        Task<StockIssueListViewModel> GetIssuesAsync(string? searchTerm, IssueStatus? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize);
        Task<StockIssue?> GetIssueByIdAsync(int id);
        Task<(bool Success, string Message, int IssueId)> CreateIssueAsync(StockIssueCreateViewModel model, string userId);
        Task<(bool Success, string Message)> UpdateIssueDraftAsync(int id, StockIssueCreateViewModel model, string userId);
        Task<(bool Success, string Message)> CancelIssueAsync(int id, string userId);
        Task<(bool Success, string Message)> ConfirmIssueAsync(int id, string userId);
        Task<string> GenerateIssueCodeAsync();
    }
}
