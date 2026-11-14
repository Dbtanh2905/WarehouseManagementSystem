using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.ViewModels
{
    public class InventoryItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public int MinimumStock { get; set; }
        public StockStatus Status { get; set; }
        public List<LocationStockViewModel> Locations { get; set; } = new();
    }

    public class LocationStockViewModel
    {
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InventoryFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public StockStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public List<InventoryItemViewModel> Items { get; set; } = new();
    }
}
