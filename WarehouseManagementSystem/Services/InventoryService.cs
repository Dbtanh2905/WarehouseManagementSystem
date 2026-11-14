using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryFilterViewModel> GetInventoryListAsync(
            string? searchTerm, int? categoryId, StockStatus? status, int pageNumber, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                    .ThenInclude(i => i.Location)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(p => p.ProductCode.ToLower().Contains(term) || p.ProductName.ToLower().Contains(term));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var productList = await query.ToListAsync();

            // Tính toán tổng tồn kho và trạng thái
            var items = productList.Select(p =>
            {
                int totalQty = p.Inventories.Sum(i => i.Quantity);
                StockStatus itemStatus = totalQty == 0 ? StockStatus.OutOfStock
                    : (totalQty <= p.MinimumStock ? StockStatus.LowStock : StockStatus.Normal);

                return new InventoryItemViewModel
                {
                    ProductId = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    CategoryName = p.Category?.CategoryName ?? "Chưa phân loại",
                    Unit = p.Unit,
                    TotalQuantity = totalQty,
                    MinimumStock = p.MinimumStock,
                    Status = itemStatus,
                    Locations = p.Inventories.Select(i => new LocationStockViewModel
                    {
                        LocationId = i.LocationId,
                        LocationCode = i.Location?.LocationCode ?? "",
                        LocationName = i.Location?.LocationName ?? "",
                        Quantity = i.Quantity,
                        UpdatedAt = i.UpdatedAt
                    }).ToList()
                };
            }).AsQueryable();

            if (status.HasValue)
            {
                items = items.Where(i => i.Status == status.Value);
            }

            int totalItems = items.Count();
            var pagedItems = items
                .OrderBy(i => i.Status == StockStatus.OutOfStock ? 0 : (i.Status == StockStatus.LowStock ? 1 : 2))
                .ThenBy(i => i.ProductCode)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new InventoryFilterViewModel
            {
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                Items = pagedItems
            };
        }

        public async Task<InventoryItemViewModel?> GetProductInventoryDetailAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                    .ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return null;

            int totalQty = product.Inventories.Sum(i => i.Quantity);
            StockStatus status = totalQty == 0 ? StockStatus.OutOfStock
                : (totalQty <= product.MinimumStock ? StockStatus.LowStock : StockStatus.Normal);

            return new InventoryItemViewModel
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                CategoryName = product.Category?.CategoryName ?? "",
                Unit = product.Unit,
                TotalQuantity = totalQty,
                MinimumStock = product.MinimumStock,
                Status = status,
                Locations = product.Inventories.Select(i => new LocationStockViewModel
                {
                    LocationId = i.LocationId,
                    LocationCode = i.Location?.LocationCode ?? "",
                    LocationName = i.Location?.LocationName ?? "",
                    Quantity = i.Quantity,
                    UpdatedAt = i.UpdatedAt
                }).ToList()
            };
        }

        public async Task<int> GetStockAtLocationAsync(int productId, int locationId)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.LocationId == locationId);
            return inv?.Quantity ?? 0;
        }

        public async Task<int> GetTotalStockAsync(int productId)
        {
            return await _context.Inventories
                .Where(i => i.ProductId == productId)
                .SumAsync(i => i.Quantity);
        }

        public async Task<List<InventoryItemViewModel>> GetLowStockItemsAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                    .ThenInclude(i => i.Location)
                .Where(p => p.IsActive)
                .ToListAsync();

            var lowStockList = new List<InventoryItemViewModel>();
            foreach (var p in products)
            {
                int total = p.Inventories.Sum(i => i.Quantity);
                if (total <= p.MinimumStock)
                {
                    lowStockList.Add(new InventoryItemViewModel
                    {
                        ProductId = p.Id,
                        ProductCode = p.ProductCode,
                        ProductName = p.ProductName,
                        CategoryName = p.Category?.CategoryName ?? "",
                        Unit = p.Unit,
                        TotalQuantity = total,
                        MinimumStock = p.MinimumStock,
                        Status = total == 0 ? StockStatus.OutOfStock : StockStatus.LowStock,
                        Locations = p.Inventories.Select(i => new LocationStockViewModel
                        {
                            LocationId = i.LocationId,
                            LocationCode = i.Location?.LocationCode ?? "",
                            LocationName = i.Location?.LocationName ?? "",
                            Quantity = i.Quantity,
                            UpdatedAt = i.UpdatedAt
                        }).ToList()
                    });
                }
            }

            return lowStockList.OrderBy(x => x.TotalQuantity).ToList();
        }

        public async Task<List<StockTransaction>> GetProductTransactionHistoryAsync(int productId)
        {
            return await _context.StockTransactions
                .Include(t => t.Location)
                .Include(t => t.PerformedBy)
                .Where(t => t.ProductId == productId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
