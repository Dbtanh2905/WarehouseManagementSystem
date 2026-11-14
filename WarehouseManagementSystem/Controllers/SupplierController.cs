using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Suppliers
                .Include(s => s.Products)
                .Include(s => s.StockReceipts)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s => s.SupplierName.ToLower().Contains(term)
                    || (s.Phone != null && s.Phone.ToLower().Contains(term))
                    || (s.Email != null && s.Email.ToLower().Contains(term))
                    || (s.Address != null && s.Address.ToLower().Contains(term)));
            }

            ViewData["CurrentSearch"] = search;
            var list = await query.OrderBy(s => s.SupplierName).ToListAsync();
            return View(list);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.Suppliers.AnyAsync(s => s.SupplierName.ToLower() == supplier.SupplierName.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("SupplierName", "Tên nhà cung cấp này đã tồn tại trong hệ thống.");
                    return View(supplier);
                }

                supplier.SupplierName = supplier.SupplierName.Trim();
                supplier.CreatedAt = DateTime.UtcNow;
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm nhà cung cấp '{supplier.SupplierName}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();

            if (ModelState.IsValid)
            {
                bool exists = await _context.Suppliers.AnyAsync(s => s.Id != id && s.SupplierName.ToLower() == supplier.SupplierName.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("SupplierName", "Tên nhà cung cấp này đã tồn tại.");
                    return View(supplier);
                }

                var existing = await _context.Suppliers.FindAsync(id);
                if (existing == null) return NotFound();

                existing.SupplierName = supplier.SupplierName.Trim();
                existing.Phone = supplier.Phone;
                existing.Email = supplier.Email;
                existing.Address = supplier.Address;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật nhà cung cấp '{existing.SupplierName}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Products)
                .Include(s => s.StockReceipts)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null) return NotFound();

            if (supplier.Products.Any() || supplier.StockReceipts.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa nhà cung cấp '{supplier.SupplierName}' do đang có {supplier.Products.Count} linh kiện hoặc {supplier.StockReceipts.Count} phiếu nhập liên kết.";
                return RedirectToAction(nameof(Index));
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa nhà cung cấp '{supplier.SupplierName}' thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
