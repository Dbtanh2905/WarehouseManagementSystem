using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.Services;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;

        public ProductController(ApplicationDbContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.ProductCode.ToLower().Contains(term) || p.ProductName.ToLower().Contains(term));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            int totalItems = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "Id", "CategoryName", categoryId);
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewData["TotalItems"] = totalItems;

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var inventoryDetail = await _inventoryService.GetProductInventoryDetailAsync(id);
            var transactions = await _inventoryService.GetProductTransactionHistoryAsync(id);

            ViewBag.InventoryDetail = inventoryDetail;
            ViewBag.Transactions = transactions;

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropDownsAsync();
            return View(new Product { MinimumStock = 10 });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                bool codeExists = await _context.Products.AnyAsync(p => p.ProductCode.ToLower() == product.ProductCode.Trim().ToLower());
                if (codeExists)
                {
                    ModelState.AddModelError("ProductCode", $"Mã linh kiện '{product.ProductCode}' đã tồn tại.");
                    await PopulateDropDownsAsync(product.CategoryId, product.SupplierId);
                    return View(product);
                }

                product.ProductCode = product.ProductCode.Trim().ToUpper();
                product.ProductName = product.ProductName.Trim();
                product.CreatedAt = DateTime.UtcNow;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm linh kiện '{product.ProductName}' ({product.ProductCode}) thành công.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDownsAsync(product.CategoryId, product.SupplierId);
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            await PopulateDropDownsAsync(product.CategoryId, product.SupplierId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                bool codeExists = await _context.Products.AnyAsync(p => p.Id != id && p.ProductCode.ToLower() == product.ProductCode.Trim().ToLower());
                if (codeExists)
                {
                    ModelState.AddModelError("ProductCode", $"Mã linh kiện '{product.ProductCode}' đã được sử dụng bởi linh kiện khác.");
                    await PopulateDropDownsAsync(product.CategoryId, product.SupplierId);
                    return View(product);
                }

                var existing = await _context.Products.FindAsync(id);
                if (existing == null) return NotFound();

                existing.ProductCode = product.ProductCode.Trim().ToUpper();
                existing.ProductName = product.ProductName.Trim();
                existing.CategoryId = product.CategoryId;
                existing.SupplierId = product.SupplierId;
                existing.Unit = product.Unit;
                existing.MinimumStock = product.MinimumStock;
                existing.Description = product.Description;
                existing.IsActive = product.IsActive;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật linh kiện '{existing.ProductName}' thành công.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDownsAsync(product.CategoryId, product.SupplierId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = !product.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã {(product.IsActive ? "kích hoạt" : "ngừng sử dụng")} linh kiện '{product.ProductCode}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Inventories)
                .Include(p => p.StockTransactions)
                .Include(p => p.StockReceiptDetails)
                .Include(p => p.StockIssueDetails)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Kiểm tra ràng buộc toàn vẹn dữ liệu
            if (product.Inventories.Any(i => i.Quantity > 0))
            {
                TempData["ErrorMessage"] = $"Không thể xóa linh kiện '{product.ProductName}' vì vẫn còn hàng tồn kho. Bạn có thể chọn Ngừng sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            if (product.StockTransactions.Any() || product.StockReceiptDetails.Any() || product.StockIssueDetails.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa linh kiện '{product.ProductName}' vì đã phát sinh lịch sử nhập/xuất kho. Hành động này sẽ làm sai lệch dữ liệu kế toán kho. Bạn có thể chọn Ngừng sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa linh kiện '{product.ProductName}' thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropDownsAsync(int? selectedCategory = null, int? selectedSupplier = null)
        {
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "Id", "CategoryName", selectedCategory);
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync(), "Id", "SupplierName", selectedSupplier);
        }
    }
}
