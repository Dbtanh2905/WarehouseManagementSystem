using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}
