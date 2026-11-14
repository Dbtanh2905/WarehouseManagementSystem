using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class WarehouseLocationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WarehouseLocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.WarehouseLocations
                .Include(l => l.Inventories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l => l.LocationCode.ToLower().Contains(term)
                    || l.LocationName.ToLower().Contains(term)
                    || (l.Description != null && l.Description.ToLower().Contains(term)));
            }

            ViewData["CurrentSearch"] = search;
            var list = await query.OrderBy(l => l.LocationCode).ToListAsync();
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
        public async Task<IActionResult> Create(WarehouseLocation location)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.WarehouseLocations.AnyAsync(l => l.LocationCode.ToLower() == location.LocationCode.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("LocationCode", $"Mã vị trí '{location.LocationCode}' đã tồn tại trong hệ thống.");
                    return View(location);
                }

                location.LocationCode = location.LocationCode.Trim().ToUpper();
                location.LocationName = location.LocationName.Trim();
                location.CreatedAt = DateTime.UtcNow;
                _context.WarehouseLocations.Add(location);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm vị trí kho '{location.LocationCode}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var location = await _context.WarehouseLocations.FindAsync(id);
            if (location == null) return NotFound();
            return View(location);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WarehouseLocation location)
        {
            if (id != location.Id) return NotFound();

            if (ModelState.IsValid)
            {
                bool exists = await _context.WarehouseLocations.AnyAsync(l => l.Id != id && l.LocationCode.ToLower() == location.LocationCode.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("LocationCode", $"Mã vị trí '{location.LocationCode}' đã được sử dụng.");
                    return View(location);
                }

                var existing = await _context.WarehouseLocations.FindAsync(id);
                if (existing == null) return NotFound();

                existing.LocationCode = location.LocationCode.Trim().ToUpper();
                existing.LocationName = location.LocationName.Trim();
                existing.Description = location.Description;
                existing.IsActive = location.IsActive;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật vị trí '{existing.LocationCode}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _context.WarehouseLocations
                .Include(l => l.Inventories)
                .Include(l => l.StockTransactions)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null) return NotFound();

            if (location.Inventories.Any(i => i.Quantity > 0))
            {
                TempData["ErrorMessage"] = $"Không thể xóa vị trí '{location.LocationCode}' vì đang còn linh kiện tồn kho tại vị trí này.";
                return RedirectToAction(nameof(Index));
            }

            if (location.StockTransactions.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa vị trí '{location.LocationCode}' vì đã có lịch sử giao dịch phát sinh. Bạn có thể chuyển sang trạng thái ngưng sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            _context.WarehouseLocations.Remove(location);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa vị trí '{location.LocationCode}' thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
